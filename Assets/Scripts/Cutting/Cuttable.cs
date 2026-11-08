using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

public class Cuttable : MonoBehaviour
{
    public enum CutType
    {
        Stem,           // Tija: se taie exact in punctul foarfecii
        Leaf,           // Frunza: se desprinde de pe tija si cade
        WrappingPaper,  // Hartia: se taie in doua bucati
        Ribbon          // Panglica: se scurteaza si lasa o fasie Scrap
    }

    [Header("Tipul obiectului taiabil")]
    public CutType cutType = CutType.Stem;

    [Header("Setari pentru Tija (Stem)")]
    [Tooltip("Lungimea minima in metri care trebuie sa ramana sus")]
    public float minLength = 0.03f;

    private bool isAlreadyScrap = false;

    public void Cut(Vector3 cutWorldPosition)
    {
        if (isAlreadyScrap || gameObject.CompareTag("Scrap")) return;

        switch (cutType)
        {
            case CutType.Stem:
                CutStemExact(cutWorldPosition);
                break;
            case CutType.Leaf:
                CutLeaf();
                break;
            case CutType.WrappingPaper:
            case CutType.Ribbon:
                CutFlatObject();
                break;
        }
    }

    private void CutStemExact(Vector3 cutWorldPosition)
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        // Dimensiunea nativa a mesh-ului pe axa Y (2.0 pt cilindru Unity, 1.0 pt cub/ProBuilder)
        Bounds meshBounds = mf.sharedMesh.bounds;
        float meshHeight = meshBounds.size.y;

        // Pozitia locala a foarfecii de-a lungul tijei
        Vector3 localCut = transform.InverseTransformPoint(cutWorldPosition);

        // Unde incepe (jos) si unde se termina (sus) mesh-ul in coordonate locale
        float localBottomY = meshBounds.min.y;
        float localTopY = meshBounds.max.y;

        // Procentul exact unde au atins lamele (0 = jos de tot, 1 = sus de tot)
        float cutRatio = Mathf.InverseLerp(localBottomY, localTopY, localCut.y);

        // Evitam taieturi in afara tijei
        if (cutRatio <= 0.02f || cutRatio >= 0.95f) return;

        float currentScaleY = transform.localScale.y;
        float remainingRatio = 1f - cutRatio; // partea de sus care ramane
        float scrapRatio = cutRatio;          // partea de jos care cade

        float newScaleY = currentScaleY * remainingRatio;
        float scrapScaleY = currentScaleY * scrapRatio;

        // Verificam lungimea reala in metri a partii care ramane
        float remainingMeters = newScaleY * meshHeight;
        if (remainingMeters < minLength) return;

        // Calculam capetele tijei in World Space INAINTE de modificare
        Vector3 worldTop = transform.TransformPoint(new Vector3(0, localTopY, 0));
        Vector3 worldBottom = transform.TransformPoint(new Vector3(0, localBottomY, 0));
        Vector3 worldCutPoint = Vector3.Lerp(worldBottom, worldTop, cutRatio);

        // Salvam frunzele copii ca sa nu se deformeze cand scalam tija!
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }
        foreach (Transform child in children)
        {
            child.SetParent(transform.parent, true);
        }

        // 1. Redimensionam si pozitionam partea de sus ramasa (intre worldCutPoint si worldTop)
        transform.localScale = new Vector3(transform.localScale.x, newScaleY, transform.localScale.z);
        transform.position = (worldTop + worldCutPoint) * 0.5f;

        // Atasarea inapoi a frunzelor (sau daca o frunza era pe bucata taiata de jos, cade si ea!)
        foreach (Transform child in children)
        {
            if (child.position.y < worldCutPoint.y)
            {
                Cuttable childCut = child.GetComponent<Cuttable>();
                if (childCut != null && childCut.cutType == CutType.Leaf)
                {
                    childCut.CutLeaf();
                }
            }
            else
            {
                child.SetParent(transform, true);
            }
        }

        // 2. Cream bucata de jos taiata (intre worldBottom si worldCutPoint)
        GameObject scrap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scrap.name = "Stem_Scrap";
        CopyMaterial(scrap);

        // Pentru un cilindru Unity (inaltime = 2), scale.y este jumatate din lungimea in metri
        float scrapLengthMeters = Vector3.Distance(worldBottom, worldCutPoint);
        scrap.transform.position = (worldBottom + worldCutPoint) * 0.5f;
        scrap.transform.rotation = transform.rotation;
        scrap.transform.localScale = new Vector3(transform.lossyScale.x, scrapLengthMeters * 0.5f, transform.lossyScale.z);

        ReplaceWithGrabBoxCollider(scrap, new Vector3(2.5f, 1.5f, 2.5f));
        SetupScrapPhysicsAndGrab(scrap);
    }

    public void CutLeaf()
    {
        if (isAlreadyScrap) return;
        isAlreadyScrap = true;

        Vector3 worldScale = transform.lossyScale;
        transform.SetParent(null, true);
        transform.localScale = worldScale;

        transform.position += Vector3.up * 0.02f;
        gameObject.name = "Leaf_Scrap";

        ReplaceWithGrabBoxCollider(gameObject, new Vector3(1.5f, 3f, 1.5f));
        SetupScrapPhysicsAndGrab(gameObject);
    }

    private void CutFlatObject()
    {
        Vector3 originalScale = transform.lossyScale;
        if (originalScale.x < 0.03f)
        {
            isAlreadyScrap = true;
            transform.SetParent(null, true);
            gameObject.name += "_Scrap";
            ReplaceWithGrabBoxCollider(gameObject, new Vector3(1.2f, 4f, 1.2f));
            SetupScrapPhysicsAndGrab(gameObject);
            return;
        }

        Vector3 halfScale = new Vector3(originalScale.x * 0.45f, originalScale.y, originalScale.z);

        transform.localScale = new Vector3(transform.localScale.x * 0.45f, transform.localScale.y, transform.localScale.z);
        transform.position -= transform.right * (originalScale.x * 0.28f);

        GameObject scrap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scrap.name = cutType.ToString() + "_Scrap";
        CopyMaterial(scrap);

        scrap.transform.position = transform.position + transform.right * (originalScale.x * 0.6f) + Vector3.up * 0.015f;
        scrap.transform.rotation = transform.rotation;
        scrap.transform.localScale = halfScale;

        ReplaceWithGrabBoxCollider(scrap, new Vector3(1f, 3f, 1f));
        SetupScrapPhysicsAndGrab(scrap);
    }

    private void CopyMaterial(GameObject target)
    {
        Renderer myRenderer = GetComponent<Renderer>();
        if (myRenderer != null)
        {
            target.GetComponent<Renderer>().sharedMaterial = myRenderer.sharedMaterial;
        }
    }

    private void ReplaceWithGrabBoxCollider(GameObject obj, Vector3 sizeMultiplier)
    {
        Collider[] oldCols = obj.GetComponents<Collider>();
        foreach (Collider c in oldCols) DestroyImmediate(c);

        BoxCollider box = obj.AddComponent<BoxCollider>();
        box.size = sizeMultiplier;
    }

    private void SetupScrapPhysicsAndGrab(GameObject obj)
    {
        obj.tag = "Scrap";

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.mass = 0.1f;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;
#else
        rb.drag = 2f;
        rb.angularDrag = 2f;
#endif

#if UNITY_6000_0_OR_NEWER
        var grabInteractable = obj.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null) grabInteractable = obj.AddComponent<XRGrabInteractable>();
#else
        var grabInteractable = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
        if (grabInteractable == null) grabInteractable = obj.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
#endif

        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grabInteractable.throwOnDetach = true;
    }
}
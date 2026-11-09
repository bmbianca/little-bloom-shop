using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

public class Cuttable : MonoBehaviour
{
    public enum CutType
    {
        Stem,           // Stem: cut exactly where the scissors close
        Leaf,           // Leaf: falls off the stem
        WrappingPaper,  // Paper: split in two
        Ribbon          // Ribbon: gets shorter and drops a Scrap piece
    }

    [Header("Cut type")]
    public CutType cutType = CutType.Stem;

    [Header("Stem settings")]
    [Tooltip("Minimum length in meters that must stay on top")]
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

        // Mesh height on Y (2 for a Unity cylinder, 1 for a cube)
        Bounds meshBounds = mf.sharedMesh.bounds;
        float meshHeight = meshBounds.size.y;

        // Scissors position along the stem, in local space
        Vector3 localCut = transform.InverseTransformPoint(cutWorldPosition);

        // Bottom and top of the mesh, in local space
        float localBottomY = meshBounds.min.y;
        float localTopY = meshBounds.max.y;

        // Where the blades hit (0 = bottom, 1 = top)
        float cutRatio = Mathf.InverseLerp(localBottomY, localTopY, localCut.y);

        // Ignore cuts outside the stem
        if (cutRatio <= 0.02f || cutRatio >= 0.95f) return;

        float currentScaleY = transform.localScale.y;
        float remainingRatio = 1f - cutRatio; // top part that stays
        float scrapRatio = cutRatio;          // bottom part that falls

        float newScaleY = currentScaleY * remainingRatio;
        float scrapScaleY = currentScaleY * scrapRatio;

        // Real length in meters of the part that stays
        float remainingMeters = newScaleY * meshHeight;
        if (remainingMeters < minLength) return;

        // Stem ends in world space, before the cut
        Vector3 worldTop = transform.TransformPoint(new Vector3(0, localTopY, 0));
        Vector3 worldBottom = transform.TransformPoint(new Vector3(0, localBottomY, 0));
        Vector3 worldCutPoint = Vector3.Lerp(worldBottom, worldTop, cutRatio);

        // Unparent the leaves so they don't stretch when the stem is scaled
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }
        foreach (Transform child in children)
        {
            child.SetParent(transform.parent, true);
        }

        // 1. Shrink the top part (from worldCutPoint to worldTop)
        transform.localScale = new Vector3(transform.localScale.x, newScaleY, transform.localScale.z);
        // keep the top end in place (works for any pivot)
        transform.position += worldTop - transform.TransformPoint(new Vector3(0, localTopY, 0));

        // Reattach the leaves (a leaf below the cut falls off)
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

        // Shorten the flower collider (on the parent) to match the cut stem
        CapsuleCollider capsule = transform.parent != null ? transform.parent.GetComponent<CapsuleCollider>() : null;
        if (capsule != null && capsule.direction == 1)
        {
            float top = capsule.center.y + capsule.height / 2f;
            float newBottom = transform.parent.InverseTransformPoint(worldCutPoint).y;
            capsule.height = top - newBottom;
            capsule.center = new Vector3(capsule.center.x, (top + newBottom) / 2f, capsule.center.z);
        }

        // 2. Create the bottom piece (from worldBottom to worldCutPoint)
        GameObject scrap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scrap.name = "Stem_Scrap";
        CopyMaterial(scrap);

        // A Unity cylinder is 2 tall, so scale.y is half the length in meters
        float scrapLengthMeters = Vector3.Distance(worldBottom, worldCutPoint);
        scrap.transform.position = (worldBottom + worldCutPoint) * 0.5f;
        scrap.transform.rotation = transform.rotation;
        // Real stem thickness (a Unity cylinder is 1 wide, the stem mesh can be thinner)
        float width = meshBounds.size.x * transform.lossyScale.x;
        float depth = meshBounds.size.z * transform.lossyScale.z;
        scrap.transform.localScale = new Vector3(width, scrapLengthMeters * 0.5f, depth);

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
        // Real size in meters (the mesh is not always a 1 m cube)
        Vector3 originalScale = transform.lossyScale;
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) originalScale = Vector3.Scale(mf.sharedMesh.bounds.size, originalScale);

        if (originalScale.x < 0.03f)
        {
            isAlreadyScrap = true;
            Transform oldParent = transform.parent;
            transform.SetParent(null, true);
            gameObject.name += "_Scrap";
            ReplaceWithGrabBoxCollider(gameObject, new Vector3(1.2f, 4f, 1.2f));
            SetupScrapPhysicsAndGrab(gameObject);

            // if this was the only visible part (e.g. the paper in PaperSheet), remove the empty parent
            if (oldParent != null && oldParent.GetComponentInChildren<Collider>() == null) Destroy(oldParent.gameObject);
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
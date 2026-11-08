using UnityEngine;

public class Cuttable : MonoBehaviour
{
    [Tooltip("Lungimea minimă sub care tija nu mai poate fi tăiată")]
    public float minLength = 0.05f;

    public void Cut(Vector3 cutWorldPosition)
    {
        // Convertim poziția foarfecii în spațiul local al tijei
        Vector3 localCutPos = transform.InverseTransformPoint(cutWorldPosition);

        // Pentru un cilindru standard în Unity (înălțime = 2 unități pe Y, de la -1 la +1)
        float currentScaleY = transform.localScale.y;

        // Calculăm unde pe tijă s-a făcut tăietura (între 0 = bază și 1 = vârf)
        // Notă: presupunem că partea de jos a tijei se taie
        float normalizedCut = Mathf.Clamp01((localCutPos.y + 1f) / 2f);

        if (normalizedCut <= 0.05f || normalizedCut >= 0.95f) return;

        float remainingRatio = 1f - normalizedCut;
        float scrapRatio = normalizedCut;

        float newScaleY = currentScaleY * remainingRatio;
        float scrapScaleY = currentScaleY * scrapRatio;

        if (newScaleY < minLength) return;

        // 1. Scurtăm tija curentă și îi ajustăm poziția ca să rămână prinsă de floare sus
        float heightDifference = currentScaleY - newScaleY;
        transform.localScale = new Vector3(transform.localScale.x, newScaleY, transform.localScale.z);
        transform.localPosition += transform.up * heightDifference;

        // 2. Creăm bucata tăiată (Scrap) care cade
        GameObject scrap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scrap.name = "Stem_Scrap";
        scrap.tag = "Scrap"; // Tag-ul din contractul cu echipa!

        // Îi copiem materialul tijei
        Renderer myRenderer = GetComponent<Renderer>();
        if (myRenderer != null)
        {
            scrap.GetComponent<Renderer>().sharedMaterial = myRenderer.sharedMaterial;
        }

        // O poziționăm în locul părții tăiate
        scrap.transform.position = cutWorldPosition;
        scrap.transform.rotation = transform.rotation;
        scrap.transform.localScale = new Vector3(transform.lossyScale.x, scrapScaleY, transform.lossyScale.z);

        // Adăugăm Rigidbody ca să cadă pe masă / în coșul de gunoi
        scrap.AddComponent<Rigidbody>();
    }
}
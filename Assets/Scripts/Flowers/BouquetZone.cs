using UnityEngine;

// Area on the assembling counter: a flower dropped here joins the bouquet.
[RequireComponent(typeof(BoxCollider))]
public class BouquetZone : MonoBehaviour
{
    public Bouquet bouquet;
    BoxCollider zone;

    void Awake()
    {
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (bouquet == null) return;

        var flower = other.GetComponentInParent<FlowerItem>();
        if (flower == null) return;

        // Only while the bouquet is still on the table, inside the zone
        if (!zone.bounds.Contains(bouquet.transform.position)) return;

        bouquet.AddFlower(flower);
    }
}
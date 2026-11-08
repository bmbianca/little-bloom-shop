using UnityEngine;

// Zona de pe masa de asamblare: o floare lăsată aici intră în buchet.
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

        // Doar dacă buchetul e încă pe masă, în zonă (nu a fost ridicat)
        if (!zone.bounds.Contains(bouquet.transform.position)) return;

        bouquet.AddFlower(flower);
    }
}
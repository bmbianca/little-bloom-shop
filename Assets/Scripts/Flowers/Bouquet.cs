using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Buchetul: lista florilor + starea ambalajului.
// D setează isWrapped / isTied / wrapColor; florile se adaugă cu AddFlower.
public class Bouquet : MonoBehaviour
{
    public List<FlowerItem> flowers = new List<FlowerItem>();
    public bool isWrapped;
    public bool isTied;
    public string wrapColor = "";

    // Înclinarea fiecărei flori în evantai (grade), în ordinea în care sunt adăugate
    static readonly Vector3[] fan =
    {
        new Vector3(0, 0, 10),
        new Vector3(0, 0, -10),
        new Vector3(-10, 0, 0),
        new Vector3(10, 0, 0),
        new Vector3(0, 0, 0),
    };

    public bool AddFlower(FlowerItem flower)
    {
        if (flower == null || flowers.Contains(flower)) return false;

        var grab = flower.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            if (grab.isSelected) return false;   // încă e ținută în mână
            grab.enabled = false;                // nu se mai poate apuca separat
        }

        var rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (var c in flower.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Se așază în buchet: tija la baza conului, înclinată în evantai
        flower.transform.SetParent(transform, false);
        flower.transform.localPosition = new Vector3(0, 0.02f, 0);
        flower.transform.localRotation = Quaternion.Euler(fan[flowers.Count % fan.Length]);

        flowers.Add(flower);
        return true;
    }
}
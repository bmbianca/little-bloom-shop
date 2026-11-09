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
        flower.transform.localRotation = Quaternion.Euler(fan[flowers.Count % fan.Length]);
        // o floare taiata are tija mai scurta: o coboram ca tija sa inceapa tot de la baza
        float stemBottom = StemBottom(flower);
        flower.transform.localPosition = new Vector3(0, 0.02f, 0) - flower.transform.localRotation * new Vector3(0, stemBottom, 0);

        flowers.Add(flower);
        return true;
    }

    // Cat de sus incepe tija fata de baza florii (0 daca nu a fost taiata).
    float StemBottom(FlowerItem flower)
    {
        Cuttable[] parts = flower.GetComponentsInChildren<Cuttable>();
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].cutType != Cuttable.CutType.Stem) continue;
            MeshFilter mf = parts[i].GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Vector3 bottom = parts[i].transform.TransformPoint(new Vector3(0, mf.sharedMesh.bounds.min.y, 0));
            return flower.transform.InverseTransformPoint(bottom).y;
        }
        return 0f;
    }
}
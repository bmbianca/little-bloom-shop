using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// The bouquet: its flowers and its wrapping state.
// The wrapping code sets isWrapped / isTied / wrapColor; flowers are added with AddFlower.
public class Bouquet : MonoBehaviour
{
    public List<FlowerItem> flowers = new List<FlowerItem>();
    public bool isWrapped;
    public bool isTied;
    public string wrapColor = "";

    // Fan tilt of each flower (degrees), in the order they are added
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
        if (isWrapped) return false;   // no new flowers once the paper is wrapped

        var grab = flower.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            if (grab.isSelected) return false;   // still held in a hand
            grab.enabled = false;                // can't be grabbed on its own anymore
        }

        var rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (var c in flower.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Place it in the bouquet: stem at the cone base, tilted like a fan
        flower.transform.SetParent(transform, false);
        flower.transform.localRotation = Quaternion.Euler(fan[flowers.Count % fan.Length]);
        // a cut flower has a shorter stem: move it down so the stem still starts at the base
        float stemBottom = StemBottom(flower);
        flower.transform.localPosition = new Vector3(0, 0.02f, 0) - flower.transform.localRotation * new Vector3(0, stemBottom, 0);

        flowers.Add(flower);
        return true;
    }

    // How high the stem starts above the flower base (0 if it was not cut).
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
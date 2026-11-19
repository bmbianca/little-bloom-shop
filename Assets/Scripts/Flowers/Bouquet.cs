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
        if (grab != null && grab.isSelected) return false;

        var rest = flower.GetComponent<FlowerRest>();
        if (rest != null) Destroy(rest);

        if (grab != null) Destroy(grab);

        var rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Destroy happens at the end of the frame. Until then the Rigidbody (with interpolation)
            // would move the flower back to where it was, so switch it off first.
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            Destroy(rb);
        }

        foreach (var c in flower.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Until the paper is wrapped, the flower lies flat on the paper
        flower.transform.SetParent(transform, false);
        flowers.Add(flower);
        LayOnPaper(flower, flowers.Count - 1);
        return true;
    }

    // Small turns left / right (degrees) for the flowers lying on the paper,
    // small enough that the stems stay on the middle of the paper (the flaps fold over them).
    static readonly float[] flatFan = { 0f, 5f, -5f, 9f, -9f, 2.5f, -2.5f };

    // Before wrapping: the flower lies on the paper, the stem pointing to the bouquet base
    // (where the ribbon goes), the head resting on the paper.
    void LayOnPaper(FlowerItem flower, int index)
    {
        Vector3 along = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;   // the bouquet direction, flat
        along = Quaternion.AngleAxis(flatFan[index % flatFan.Length], Vector3.up) * along;
        Vector3 stemStart = transform.position + Vector3.up * 0.006f;   // just above the paper
        float paperTop = transform.position.y + 0.004f;

        // a cut flower has a shorter stem: the stem still starts at the base
        float stemBottom = StemBottom(flower);
        flower.transform.rotation = Quaternion.LookRotation(Vector3.up, along);   // stem along the table
        flower.transform.position = stemStart - along * stemBottom;

        // the head is thicker than the stem: tilt the flower up a little so the head rests on the paper
        Transform head = flower.transform.Find("Head");
        Vector3 headCenter = head != null ? HeadBounds(head).center : flower.transform.position + along * 0.5f;
        float sink = paperTop - LowestPoint(flower);
        float headDistance = Vector3.Distance(Flat(headCenter), Flat(stemStart));
        if (sink > 0f && headDistance > 0.01f)
        {
            Vector3 side = Vector3.Cross(Vector3.up, along);
            float angle = Mathf.Atan2(sink, headDistance) * Mathf.Rad2Deg;
            flower.transform.RotateAround(stemStart, side, -angle);
        }

        // anything still under the paper: lift the whole flower a bit
        float stillUnder = paperTop - LowestPoint(flower);
        if (stillUnder > 0f) flower.transform.position += Vector3.up * stillUnder;
    }

    // When the paper is wrapped: the flowers go into the cone, stems at the base, tilted like a fan.
    public void ArrangeInCone()
    {
        for (int i = 0; i < flowers.Count; i++)
        {
            FlowerItem flower = flowers[i];
            if (flower == null) continue;
            flower.transform.localRotation = Quaternion.Euler(fan[i % fan.Length]);
            // a cut flower has a shorter stem: move it down so the stem still starts at the base
            float stemBottom = StemBottom(flower);
            flower.transform.localPosition = new Vector3(0, 0.02f, 0) - flower.transform.localRotation * new Vector3(0, stemBottom, 0);
        }
    }

    static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    static Bounds HeadBounds(Transform head)
    {
        Renderer[] parts = head.GetComponentsInChildren<Renderer>();
        Bounds b = parts.Length > 0 ? parts[0].bounds : new Bounds(head.position, Vector3.zero);
        for (int i = 1; i < parts.Length; i++) b.Encapsulate(parts[i].bounds);
        return b;
    }

    static float LowestPoint(FlowerItem flower)
    {
        float lowest = float.MaxValue;
        foreach (Renderer r in flower.GetComponentsInChildren<Renderer>())
            lowest = Mathf.Min(lowest, r.bounds.min.y);
        return lowest == float.MaxValue ? flower.transform.position.y : lowest;
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
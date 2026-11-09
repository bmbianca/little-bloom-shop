using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// One flap of the paper on the table (left or right).
// This object sits on the flap hinge. Grab the flap and lift it over the flowers:
// it rotates with your hand. If it goes far enough it stays folded,
// otherwise it falls back.
// It stops where it meets the other flap (a triangle),
// or earlier if it would touch a flower in the bouquet.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PaperFlap : MonoBehaviour
{
    public WrappingStation station;
    public Vector3 openDirection = Vector3.back;   // toward the free edge of the flap when it lies flat
    public GameObject tab;                         // marker that shows the flap can be folded

    public float foldedAngle = 130f;   // angle where it meets the other flap (triangle)
    public float snapAngle = 100f;     // above this angle it stays folded on release
    public float handTravel = 0.3f;    // hand distance (m) for a full fold
    public float width = 0.12f;        // flap width, from hinge to edge
    public float length = 0.4f;        // flap length, along the hinge

    [Header("State (read only)")]
    public bool canFold;
    public bool isFolded;

    XRSimpleInteractable handle;
    IXRSelectInteractor hand;
    Quaternion openRotation;
    Vector3 axis;
    float angle;
    float maxAngle;      // where the flap stops this time
    float angleAtGrab;
    Vector3 handAtGrab;

    void Awake()
    {
        handle = GetComponent<XRSimpleInteractable>();
        handle.selectEntered.AddListener(OnGrab);
        handle.selectExited.AddListener(OnRelease);

        openRotation = transform.rotation;
        openDirection = openDirection.normalized;
        // hinge axis: a positive rotation lifts the flap
        axis = Vector3.Cross(openDirection, Vector3.up).normalized;
    }

    void OnDestroy()
    {
        if (handle != null)
        {
            handle.selectEntered.RemoveListener(OnGrab);
            handle.selectExited.RemoveListener(OnRelease);
        }
    }

    void Update()
    {
        if (tab != null) tab.SetActive(canFold && !isFolded && hand == null);

        if (hand == null) return;

        // hand goes up and over the flowers -> the flap folds
        Vector3 moved = hand.transform.position - handAtGrab;
        float distance = Vector3.Dot(moved, Vector3.up) + Vector3.Dot(moved, -openDirection);
        angle = angleAtGrab + distance / handTravel * foldedAngle;
        angle = Mathf.Clamp(angle, 0f, maxAngle);
        ApplyAngle();
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (!canFold || isFolded) return;

        StopAllCoroutines();
        hand = args.interactorObject;
        handAtGrab = hand.transform.position;
        angleAtGrab = angle;
        maxAngle = StopAngle();
    }

    // How far the flap can fold: up to the triangle, or until it almost touches a flower.
    float StopAngle()
    {
        float stop = foldedAngle;
        if (station == null || station.bouquet == null) return stop;

        Vector3 hinge = transform.position;
        for (int f = 0; f < station.bouquet.flowers.Count; f++)
        {
            Renderer[] parts = station.bouquet.flowers[f].GetComponentsInChildren<Renderer>();
            for (int r = 0; r < parts.Length; r++)
            {
                // 5 points along each flower part (stem or head), end to end
                // (taken from the mesh, so any shape works, not only a Unity cylinder)
                MeshFilter mf = parts[r].GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                Transform part = parts[r].transform;
                for (int k = -2; k <= 2; k++)
                {
                    Vector3 local = mb.center + Vector3.up * (mb.extents.y * k / 2f);
                    Vector3 p = part.TransformPoint(local) - hinge;

                    // is the point in front of the flap (along the hinge)?
                    if (Mathf.Abs(Vector3.Dot(p, axis)) > length / 2f) continue;

                    // distance from the hinge, seen from the flap end
                    float side = Vector3.Dot(p, openDirection);
                    float up = p.y;
                    if (up <= 0f || side * side + up * up > width * width) continue;   // the flap can't reach it

                    // angle where the flap would touch the point (minus a small gap)
                    float touch = Mathf.Atan2(up, side) * Mathf.Rad2Deg - 5f;
                    if (touch < stop) stop = touch;
                }
            }
        }
        return Mathf.Max(stop, 10f);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (hand == null) return;
        IXRSelectInteractor releasedHand = hand;
        hand = null;

        if (angle >= Mathf.Min(snapAngle, maxAngle - 5f))
        {
            StartCoroutine(MoveTo(maxAngle, true));

            HapticImpulsePlayer haptics = releasedHand.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (haptics != null) haptics.SendHapticImpulse(0.3f, 0.06f);
        }
        else
        {
            StartCoroutine(MoveTo(0f, false));
        }
    }

    IEnumerator MoveTo(float target, bool fold)
    {
        while (Mathf.Abs(angle - target) > 0.5f)
        {
            angle = Mathf.MoveTowards(angle, target, 400f * Time.deltaTime);
            ApplyAngle();
            yield return null;
        }
        angle = target;
        ApplyAngle();

        if (fold)
        {
            isFolded = true;
            handle.enabled = false;   // a folded flap can't be grabbed again
            if (station != null) station.OnFlapFolded();
        }
    }

    void ApplyAngle()
    {
        transform.rotation = Quaternion.AngleAxis(angle, axis) * openRotation;
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

public class ScissorsController : MonoBehaviour
{
    [Header("Blade halves")]
    public Transform halfA;
    public Transform halfB;

    [Header("Rotation")]
    public Vector3 openRotationA = new Vector3(0, -10f, 0);
    public Vector3 openRotationB = new Vector3(0, 10f, 0);
    public Vector3 closedRotationA = Vector3.zero;
    public Vector3 closedRotationB = Vector3.zero;

    [Header("Cut detection")]
    public Collider cutZoneTrigger;

#if UNITY_6000_0_OR_NEWER
    private XRGrabInteractable grabInteractable;
#else
    private UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable grabInteractable;
#endif

    void Awake()
    {
#if UNITY_6000_0_OR_NEWER
        grabInteractable = GetComponent<XRGrabInteractable>();
#else
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
#endif

        if (grabInteractable != null)
        {
            grabInteractable.activated.AddListener(OnScissorsClosed);
            grabInteractable.deactivated.AddListener(OnScissorsOpened);
        }
    }

    public void OnScissorsClosed(ActivateEventArgs args)
    {
        if (halfA != null) halfA.localEulerAngles = closedRotationA;
        if (halfB != null) halfB.localEulerAngles = closedRotationB;

        if (cutZoneTrigger == null) return;

        // Center of the cut zone in world space
        Vector3 exactCutPoint = cutZoneTrigger.bounds.center;

        // Real cut zone box, rotated with the scissors
        // (bounds.extents is world-aligned; rotating it again gives a bigger, wrong box)
        Vector3 halfSize = cutZoneTrigger.bounds.extents;
        BoxCollider box = cutZoneTrigger as BoxCollider;
        if (box != null)
        {
            exactCutPoint = box.transform.TransformPoint(box.center);
            halfSize = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
            halfSize = new Vector3(Mathf.Abs(halfSize.x), Mathf.Abs(halfSize.y), Mathf.Abs(halfSize.z));
        }

        // Find every collider in the zone, triggers included
        Collider[] hits = Physics.OverlapBox(
            exactCutPoint,
            halfSize,
            cutZoneTrigger.transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        Cuttable leafCandidate = null;
        Cuttable otherCandidate = null;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.CompareTag("Scrap")) continue;

            // 1. Check the object that was hit
            Cuttable c = hit.GetComponent<Cuttable>();
            if (c != null && !c.CompareTag("Scrap"))
            {
                if (c.cutType == Cuttable.CutType.Leaf)
                {
                    leafCandidate = c;
                    break;
                }
                else if (otherCandidate == null)
                {
                    otherCandidate = c;
                }
            }

            // 2. If it is a stem, check for a leaf right next to the blades
            Cuttable[] childCuttables = hit.GetComponentsInChildren<Cuttable>();
            foreach (Cuttable child in childCuttables)
            {
                if (child.cutType == Cuttable.CutType.Leaf && !child.CompareTag("Scrap"))
                {
                    if (Vector3.Distance(child.transform.position, exactCutPoint) < 0.06f)
                    {
                        leafCandidate = child;
                        break;
                    }
                }
            }
        }

        if (leafCandidate != null)
        {
            leafCandidate.Cut(exactCutPoint);
        }
        else if (otherCandidate != null)
        {
            otherCandidate.Cut(exactCutPoint);
        }
    }

    public void OnScissorsOpened(DeactivateEventArgs args)
    {
        if (halfA != null) halfA.localEulerAngles = openRotationA;
        if (halfB != null) halfB.localEulerAngles = openRotationB;
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

public class ScissorsController : MonoBehaviour
{
    [Header("Referinte Jumatati")]
    public Transform halfA;
    public Transform halfB;

    [Header("Setari Rotatie")]
    public Vector3 openRotationA = new Vector3(0, -10f, 0);
    public Vector3 openRotationB = new Vector3(0, 10f, 0);
    public Vector3 closedRotationA = Vector3.zero;
    public Vector3 closedRotationB = Vector3.zero;

    [Header("Detectie Taiere")]
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

        // Folosim centrul real al BoxCollider-ului din lume!
        Vector3 exactCutPoint = cutZoneTrigger.bounds.center;

        // Detectam toate colliderele (inclusiv daca vreunul e setat pe Trigger)
        Collider[] hits = Physics.OverlapBox(
            exactCutPoint,
            cutZoneTrigger.bounds.extents,
            cutZoneTrigger.transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        Cuttable leafCandidate = null;
        Cuttable otherCandidate = null;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.CompareTag("Scrap")) continue;

            // 1. Verificam direct pe obiectul lovit
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

            // 2. Daca am lovit tija, verificam daca are o frunza copil chiar langa lame!
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
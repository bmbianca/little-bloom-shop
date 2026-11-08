using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ScissorsController : MonoBehaviour
{
    [Header("Referințe Jumătăți")]
    public Transform halfA;
    public Transform halfB;

    [Header("Setări Rotație")]
    public Vector3 openRotationA = new Vector3(0, -15f, 0);
    public Vector3 openRotationB = new Vector3(0, 15f, 0);
    public Vector3 closedRotationA = Vector3.zero;
    public Vector3 closedRotationB = Vector3.zero;

    [Header("Detecție Tăiere")]
    public Collider cutZoneTrigger; // Box Collider-ul cu IsTrigger de pe CutZone

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Când apeși Trigger-ul pe controller
        grabInteractable.activated.AddListener(OnScissorsClosed);
        // Când eliberezi Trigger-ul
        grabInteractable.deactivated.AddListener(OnScissorsOpened);
    }

    private void OnScissorsClosed(ActivateEventArgs args)
    {
        // 1. Închidem vizual lamele
        if (halfA != null) halfA.localEulerAngles = closedRotationA;
        if (halfB != null) halfB.localEulerAngles = closedRotationB;

        // 2. Verificăm ce obiecte Cuttable se află între lame în acest moment
        Collider[] hits = Physics.OverlapBox(
            cutZoneTrigger.bounds.center,
            cutZoneTrigger.bounds.extents,
            cutZoneTrigger.transform.rotation
        );

        foreach (Collider hit in hits)
        {
            Cuttable cuttable = hit.GetComponentInParent<Cuttable>();
            if (cuttable != null)
            {
                cuttable.Cut(cutZoneTrigger.transform.position);
                break; // Tăiem un obiect pe apăsare
            }
        }
    }

    private void OnScissorsOpened(DeactivateEventArgs args)
    {
        // Deschidem lamele înapoi
        if (halfA != null) halfA.localEulerAngles = openRotationA;
        if (halfB != null) halfB.localEulerAngles = openRotationB;
    }
}
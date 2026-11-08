using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Floarea stă blocată în vază până o apucă cineva prima dată.
[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class FlowerRest : MonoBehaviour
{
    Rigidbody rb;
    XRGrabInteractable grab;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDestroy()
    {
        if (grab != null) grab.selectEntered.RemoveListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        rb.constraints = RigidbodyConstraints.None;
    }
}
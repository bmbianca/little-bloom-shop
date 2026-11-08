using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Socket care primeste doar hartie (pe masa) sau doar panglica (la buchet).
public class WrapSocket : XRSocketInteractor
{
    public bool acceptRibbon;   // false = primeste hartie, true = primeste panglica

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return base.CanHover(interactable) && Accepts(interactable.transform);
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return base.CanSelect(interactable) && Accepts(interactable.transform);
    }

    bool Accepts(Transform t)
    {
        WrapItem item = t.GetComponent<WrapItem>();
        return item != null && item.isRibbon == acceptRibbon;
    }
}

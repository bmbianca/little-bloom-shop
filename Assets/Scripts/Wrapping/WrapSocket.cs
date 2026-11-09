using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Socket that takes only paper (on the table) or only ribbon (at the bouquet).
public class WrapSocket : XRSocketInteractor
{
    public bool acceptRibbon;   // false = takes paper, true = takes ribbon

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

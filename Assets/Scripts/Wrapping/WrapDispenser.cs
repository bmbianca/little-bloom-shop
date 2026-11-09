using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// On a paper roll or a ribbon spool.
// Hold grip on it -> a new piece (paper or ribbon) appears in your hand.
// A quick touch and release does nothing.
[RequireComponent(typeof(XRSimpleInteractable))]
public class WrapDispenser : MonoBehaviour
{
    public GameObject itemPrefab;     // PaperSheet or RibbonPiece
    public Material itemMaterial;     // color of the piece
    public string colorName = "";

    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnSelected);
    }

    void OnDestroy()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(OnSelected);
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        StartCoroutine(GiveItem(args.interactorObject));
    }

    IEnumerator GiveItem(IXRSelectInteractor hand)
    {
        // wait one frame, so we don't change the selection inside the event
        yield return null;

        XRInteractionManager manager = interactable.interactionManager;
        if (manager == null || itemPrefab == null) yield break;

        // if the player already let go, give nothing
        if (!hand.isSelectActive) yield break;

        // The swap roll -> new piece happens in the same frame.
        // After SelectExit the hand has no selection, so isSelectActive is not
        // checked again between SelectExit and SelectEnter.

        // the new piece appears in the hand
        Transform handTransform = hand.transform;
        GameObject item = Instantiate(itemPrefab, handTransform.position, handTransform.rotation);

        WrapItem wrapItem = item.GetComponent<WrapItem>();
        if (wrapItem != null) wrapItem.colorName = colorName;

        Renderer itemRenderer = item.GetComponentInChildren<Renderer>();
        if (itemRenderer != null && itemMaterial != null) itemRenderer.sharedMaterial = itemMaterial;

        // the hand lets go of the roll...
        if (interactable.isSelected)
        {
            manager.SelectExit(hand, interactable);
        }

        // ...and grabs the new piece
        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            manager.SelectEnter(hand, (IXRSelectInteractable)grab);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Pe o rola de hartie sau pe o bobina de panglica.
// Tii apasat pe rola -> tragi o bucata noua (foaie sau panglica) si o ai in mana.
// Daca doar atingi rola si dai drumul imediat, nu se intampla nimic.
[RequireComponent(typeof(XRSimpleInteractable))]
public class WrapDispenser : MonoBehaviour
{
    public GameObject itemPrefab;     // PaperSheet sau RibbonPiece
    public Material itemMaterial;     // culoarea bucatii
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
        // asteptam un frame, ca sa nu schimbam selectia chiar in timpul evenimentului
        yield return null;

        XRInteractionManager manager = interactable.interactionManager;
        if (manager == null || itemPrefab == null) yield break;

        // daca jucatorul a dat deja drumul, nu tragem nicio foaie
        if (!hand.isSelectActive) yield break;

        // Schimbul rola -> bucata noua se face tot in acelasi frame.
        // Important: dupa SelectExit, mana "uita" ca butonul e tinut (nu mai are selectie),
        // asa ca NU mai verificam isSelectActive intre SelectExit si SelectEnter.

        // bucata noua apare in mana
        Transform handTransform = hand.transform;
        GameObject item = Instantiate(itemPrefab, handTransform.position, handTransform.rotation);

        WrapItem wrapItem = item.GetComponent<WrapItem>();
        if (wrapItem != null) wrapItem.colorName = colorName;

        Renderer itemRenderer = item.GetComponentInChildren<Renderer>();
        if (itemRenderer != null && itemMaterial != null) itemRenderer.sharedMaterial = itemMaterial;

        // mana da drumul rolei...
        if (interactable.isSelected)
        {
            manager.SelectExit(hand, interactable);
        }

        // ...si prinde imediat bucata noua
        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            manager.SelectEnter(hand, (IXRSelectInteractable)grab);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Something you press with your hand (Grip): the desk bell, the notebook, the door sign.
// When pressed it calls onPressed (wired to the GameLoop).
[RequireComponent(typeof(XRSimpleInteractable))]
public class TouchButton : MonoBehaviour
{
    public UnityEvent onPressed = new UnityEvent();

    [Header("Optional")]
    public Transform pressPart;        // moves down a little when pressed (the bell button)
    public float pressDepth = 0.008f;
    public AudioClip sound;
    public float cooldown = 0.5f;      // ignore double presses

    XRSimpleInteractable interactable;
    Vector3 pressPartRest;
    float lastPress = -10f;
    Coroutine pressAnimation;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnSelected);
        if (pressPart != null) pressPartRest = pressPart.localPosition;
    }

    void OnDestroy()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(OnSelected);
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        // With Grab Mode "Toggle" the hand would stay on the button until the next press,
        // and that press would only let go. So the button lets go of the hand by itself.
        XRBaseInputInteractor hand = args.interactorObject as XRBaseInputInteractor;
        if (hand != null && (hand.selectActionTrigger == XRBaseInputInteractor.InputTriggerType.Toggle ||
                             hand.selectActionTrigger == XRBaseInputInteractor.InputTriggerType.Sticky))
        {
            StartCoroutine(LetGo(args.interactorObject));
        }

        if (Time.time - lastPress < cooldown) return;
        lastPress = Time.time;

        if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position);
        if (pressPart != null)
        {
            if (pressAnimation != null) StopCoroutine(pressAnimation);
            pressAnimation = StartCoroutine(PressAnimation());
        }

        onPressed.Invoke();
    }

    // one frame later, so the selection is not changed in the middle of the select event
    IEnumerator LetGo(IXRSelectInteractor hand)
    {
        yield return null;
        if (interactable.interactionManager != null && interactable.interactorsSelecting.Contains(hand))
        {
            interactable.interactionManager.SelectExit(hand, (IXRSelectInteractable)interactable);
        }
    }

    IEnumerator PressAnimation()
    {
        pressPart.localPosition = pressPartRest + Vector3.down * pressDepth;
        yield return new WaitForSeconds(0.15f);
        pressPart.localPosition = pressPartRest;
    }
}

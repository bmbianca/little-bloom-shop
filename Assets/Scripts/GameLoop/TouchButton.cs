using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
        if (Time.time - lastPress < cooldown) return;
        lastPress = Time.time;

        if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position);
        if (pressPart != null)
        {
            StopAllCoroutines();
            StartCoroutine(PressAnimation());
        }

        onPressed.Invoke();
    }

    IEnumerator PressAnimation()
    {
        pressPart.localPosition = pressPartRest + Vector3.down * pressDepth;
        yield return new WaitForSeconds(0.15f);
        pressPart.localPosition = pressPartRest;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A paper roll on the wall rack.
// Grab the hanging edge and pull: the paper unrolls after your hand.
// After a full sheet it tears off and stays in your hand.
// Let go earlier and the paper rolls back.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PaperRoll : MonoBehaviour
{
    [Header("Paper")]
    public GameObject sheetPrefab;      // PaperSheet
    public Material paperMaterial;
    public string colorName = "";

    [Header("Roll parts")]
    public Transform pullOrigin;        // roll edge where the paper comes out
    public Transform pulledPaper;       // the unrolling paper (its child is the sheet)
    public Transform rollVisual;        // the spinning cylinder

    [Header("Sizes (meters)")]
    public float sheetLength = 0.5f;    // length of a full sheet
    public float restLength = 0.1f;     // hanging length when not pulled
    public float rollRadius = 0.035f;

    XRSimpleInteractable handle;
    IXRSelectInteractor hand;
    float startDistance;
    float currentLength;
    Transform paperPiece;

    void Awake()
    {
        handle = GetComponent<XRSimpleInteractable>();
        handle.selectEntered.AddListener(OnGrab);
        handle.selectExited.AddListener(OnRelease);

        paperPiece = pulledPaper.GetChild(0);
        currentLength = restLength;
        SetLength(restLength, Vector3.down);
    }

    void OnDestroy()
    {
        if (handle != null)
        {
            handle.selectEntered.RemoveListener(OnGrab);
            handle.selectExited.RemoveListener(OnRelease);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        StopAllCoroutines();
        hand = args.interactorObject;
        // hand distance at grab time; pulling starts from here
        startDistance = Vector3.Distance(pullOrigin.position, hand.transform.position);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (hand == null) return;
        hand = null;
        StartCoroutine(RollBack());
    }

    void Update()
    {
        if (hand == null) return;

        Vector3 toHand = hand.transform.position - pullOrigin.position;
        float distance = toHand.magnitude;
        float length = restLength + (distance - startDistance);
        length = Mathf.Clamp(length, restLength, sheetLength);

        // paper follows the hand but does not go into the wall
        Vector3 direction = toHand.normalized;
        if (Vector3.Dot(direction, pullOrigin.forward) < 0f)
        {
            direction = Vector3.ProjectOnPlane(direction, pullOrigin.forward).normalized;
        }
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.down;

        SpinRoll(length - currentLength);
        currentLength = length;
        SetLength(length, direction);

        if (length >= sheetLength)
        {
            Tear();
        }
    }

    // ---------- the sheet tears off and stays in the hand ----------
    void Tear()
    {
        IXRSelectInteractor tearingHand = hand;
        hand = null;

        GameObject sheet = Instantiate(sheetPrefab, pulledPaper.position, pulledPaper.rotation);

        WrapItem item = sheet.GetComponent<WrapItem>();
        if (item != null) item.colorName = colorName;

        Renderer sheetRenderer = sheet.GetComponentInChildren<Renderer>();
        if (sheetRenderer != null && paperMaterial != null) sheetRenderer.sharedMaterial = paperMaterial;

        // only the hanging edge stays on the roll
        currentLength = restLength;
        SetLength(restLength, Vector3.down);

        // the hand lets go of the roll and grabs the sheet in the same frame
        XRInteractionManager manager = handle.interactionManager;
        if (handle.isSelected)
        {
            manager.SelectExit(tearingHand, handle);
        }
        XRGrabInteractable grab = sheet.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            manager.SelectEnter(tearingHand, (IXRSelectInteractable)grab);
        }

        // small vibration for the tear
        HapticImpulsePlayer haptics = tearingHand.transform.GetComponentInParent<HapticImpulsePlayer>();
        if (haptics != null) haptics.SendHapticImpulse(0.4f, 0.08f);
    }

    // ---------- the paper rolls back ----------
    IEnumerator RollBack()
    {
        while (currentLength > restLength + 0.001f)
        {
            float newLength = Mathf.MoveTowards(currentLength, restLength, 1.5f * Time.deltaTime);
            SpinRoll(newLength - currentLength);
            currentLength = newLength;
            SetLength(currentLength, Vector3.Lerp(-pulledPaper.up, Vector3.down, 0.2f));
            yield return null;
        }
        SetLength(restLength, Vector3.down);
    }

    // paper starts at the roll edge, with the given length and direction
    void SetLength(float length, Vector3 direction)
    {
        pulledPaper.position = pullOrigin.position;
        Vector3 facing = Vector3.ProjectOnPlane(pullOrigin.forward, direction);
        if (facing.sqrMagnitude < 0.001f) facing = pullOrigin.forward;
        pulledPaper.rotation = Quaternion.LookRotation(facing, -direction);

        Vector3 scale = paperPiece.localScale;
        scale.y = length;
        paperPiece.localScale = scale;
        paperPiece.localPosition = new Vector3(0f, -length / 2f, 0f);
    }

    void SpinRoll(float pulled)
    {
        if (rollVisual == null) return;
        float degrees = pulled / rollRadius * Mathf.Rad2Deg;
        rollVisual.Rotate(Vector3.up, degrees, Space.Self);   // the cylinder lies down, its axis is local Y
    }
}

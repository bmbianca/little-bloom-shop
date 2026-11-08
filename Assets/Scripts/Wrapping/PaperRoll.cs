using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// O rola de hartie de pe suportul de pe perete.
// Apuci marginea care atarna si tragi: hartia se desfasoara dupa mana.
// Cand ai tras o foaie intreaga, se rupe si ramane in mana ta.
// Daca dai drumul mai devreme, hartia se ruleaza inapoi.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PaperRoll : MonoBehaviour
{
    [Header("Hartia")]
    public GameObject sheetPrefab;      // PaperSheet
    public Material paperMaterial;
    public string colorName = "";

    [Header("Partile rolei")]
    public Transform pullOrigin;        // marginea rolei de unde iese hartia
    public Transform pulledPaper;       // hartia care se desfasoara (copilul ei e foaia)
    public Transform rollVisual;        // cilindrul care se invarte

    [Header("Marimi (metri)")]
    public float sheetLength = 0.5f;    // cat are o foaie intreaga
    public float restLength = 0.1f;     // cat atarna cand nu tragi
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
        // cat de departe era mana cand a apucat; tragem de la distanta asta incolo
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

        // hartia merge spre mana, dar nu intra in perete
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

    // ---------- foaia se rupe si ramane in mana ----------
    void Tear()
    {
        IXRSelectInteractor tearingHand = hand;
        hand = null;

        GameObject sheet = Instantiate(sheetPrefab, pulledPaper.position, pulledPaper.rotation);

        WrapItem item = sheet.GetComponent<WrapItem>();
        if (item != null) item.colorName = colorName;

        Renderer sheetRenderer = sheet.GetComponentInChildren<Renderer>();
        if (sheetRenderer != null && paperMaterial != null) sheetRenderer.sharedMaterial = paperMaterial;

        // pe rola ramane iar doar marginea care atarna
        currentLength = restLength;
        SetLength(restLength, Vector3.down);

        // mana lasa rola si prinde foaia, in acelasi frame
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

        // o mica vibratie: "ruptul" hartiei
        HapticImpulsePlayer haptics = tearingHand.transform.GetComponentInParent<HapticImpulsePlayer>();
        if (haptics != null) haptics.SendHapticImpulse(0.4f, 0.08f);
    }

    // ---------- hartia se ruleaza inapoi ----------
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

    // hartia incepe la marginea rolei si are lungimea data, spre directia data
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
        rollVisual.Rotate(Vector3.up, degrees, Space.Self);   // cilindrul e culcat, axa lui e Y local
    }
}

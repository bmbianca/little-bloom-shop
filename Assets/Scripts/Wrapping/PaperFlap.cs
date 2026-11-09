using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// O clapa a foii de pe masa (stanga sau dreapta).
// Obiectul asta sta pe "balamaua" clapei. Apuci clapa si o ridici peste flori:
// clapa se roteste dupa mana ta. Daca ai dus-o destul de departe, se fixeaza;
// daca nu, cade inapoi.
// Clapa se opreste unde se intalneste cu cealalta (forma de triunghi)
// sau mai devreme, daca ar atinge o floare din buchet.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PaperFlap : MonoBehaviour
{
    public WrappingStation station;
    public Vector3 openDirection = Vector3.back;   // spre marginea libera a clapei, cand e intinsa
    public GameObject tab;                         // semnul care arata ca poti indoi clapa

    public float foldedAngle = 130f;   // unghiul la care se intalneste cu cealalta clapa (triunghi)
    public float snapAngle = 100f;     // de la cat in sus se fixeaza cand dai drumul
    public float handTravel = 0.3f;    // cati metri misti mana pentru o indoire intreaga
    public float width = 0.12f;        // cat de lata e clapa (de la balama la margine)
    public float length = 0.4f;        // cat de lunga e clapa (de-a lungul balamalei)

    [Header("Stare (doar de citit)")]
    public bool canFold;
    public bool isFolded;

    XRSimpleInteractable handle;
    IXRSelectInteractor hand;
    Quaternion openRotation;
    Vector3 axis;
    float angle;
    float maxAngle;      // unde se opreste clapa de data asta
    float angleAtGrab;
    Vector3 handAtGrab;

    void Awake()
    {
        handle = GetComponent<XRSimpleInteractable>();
        handle.selectEntered.AddListener(OnGrab);
        handle.selectExited.AddListener(OnRelease);

        openRotation = transform.rotation;
        openDirection = openDirection.normalized;
        // axa balamalei: o rotire pozitiva ridica clapa de pe masa
        axis = Vector3.Cross(openDirection, Vector3.up).normalized;
    }

    void OnDestroy()
    {
        if (handle != null)
        {
            handle.selectEntered.RemoveListener(OnGrab);
            handle.selectExited.RemoveListener(OnRelease);
        }
    }

    void Update()
    {
        if (tab != null) tab.SetActive(canFold && !isFolded && hand == null);

        if (hand == null) return;

        // mana urca si trece peste flori -> clapa se ridica si se indoaie
        Vector3 moved = hand.transform.position - handAtGrab;
        float distance = Vector3.Dot(moved, Vector3.up) + Vector3.Dot(moved, -openDirection);
        angle = angleAtGrab + distance / handTravel * foldedAngle;
        angle = Mathf.Clamp(angle, 0f, maxAngle);
        ApplyAngle();
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (!canFold || isFolded) return;

        StopAllCoroutines();
        hand = args.interactorObject;
        handAtGrab = hand.transform.position;
        angleAtGrab = angle;
        maxAngle = StopAngle();
    }

    // Cat de mult se poate indoi clapa: pana la triunghi, sau pana aproape atinge o floare.
    float StopAngle()
    {
        float stop = foldedAngle;
        if (station == null || station.bouquet == null) return stop;

        Vector3 hinge = transform.position;
        for (int f = 0; f < station.bouquet.flowers.Count; f++)
        {
            Renderer[] parts = station.bouquet.flowers[f].GetComponentsInChildren<Renderer>();
            for (int r = 0; r < parts.Length; r++)
            {
                // 5 puncte de-a lungul bucatii de floare (tija sau capul), de la un capat la altul
                // (luate din mesh, ca sa mearga pentru orice forma, nu doar pentru cilindrul Unity)
                MeshFilter mf = parts[r].GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                Transform part = parts[r].transform;
                for (int k = -2; k <= 2; k++)
                {
                    Vector3 local = mb.center + Vector3.up * (mb.extents.y * k / 2f);
                    Vector3 p = part.TransformPoint(local) - hinge;

                    // punctul e in dreptul clapei (de-a lungul balamalei)?
                    if (Mathf.Abs(Vector3.Dot(p, axis)) > length / 2f) continue;

                    // cat de departe e de balama, privit din capatul clapei
                    float side = Vector3.Dot(p, openDirection);
                    float up = p.y;
                    if (up <= 0f || side * side + up * up > width * width) continue;   // clapa nu ajunge pana acolo

                    // unghiul la care clapa ar atinge punctul (minus o mica distanta)
                    float touch = Mathf.Atan2(up, side) * Mathf.Rad2Deg - 5f;
                    if (touch < stop) stop = touch;
                }
            }
        }
        return Mathf.Max(stop, 10f);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (hand == null) return;
        IXRSelectInteractor releasedHand = hand;
        hand = null;

        if (angle >= Mathf.Min(snapAngle, maxAngle - 5f))
        {
            StartCoroutine(MoveTo(maxAngle, true));

            HapticImpulsePlayer haptics = releasedHand.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (haptics != null) haptics.SendHapticImpulse(0.3f, 0.06f);
        }
        else
        {
            StartCoroutine(MoveTo(0f, false));
        }
    }

    IEnumerator MoveTo(float target, bool fold)
    {
        while (Mathf.Abs(angle - target) > 0.5f)
        {
            angle = Mathf.MoveTowards(angle, target, 400f * Time.deltaTime);
            ApplyAngle();
            yield return null;
        }
        angle = target;
        ApplyAngle();

        if (fold)
        {
            isFolded = true;
            handle.enabled = false;   // clapa fixata nu se mai apuca
            if (station != null) station.OnFlapFolded();
        }
    }

    void ApplyAngle()
    {
        transform.rotation = Quaternion.AngleAxis(angle, axis) * openRotation;
    }
}

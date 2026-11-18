using System.Collections;
using UnityEngine;

// The OPEN / CLOSED sign on the door. Flips 180 degrees to show the other side.
// If it shows the wrong side, tick or untick "Shows Open In Scene".
public class ShopSign : MonoBehaviour
{
    public bool showsOpenInScene = true;   // which side faces the player as the sign is placed in the scene
    public float flipDuration = 0.4f;

    [Header("State (read only)")]
    public bool isOpen;

    Quaternion sceneRotation;

    void Awake()
    {
        sceneRotation = transform.localRotation;
        isOpen = showsOpenInScene;
    }

    public void SetOpen(bool open, bool instant)
    {
        isOpen = open;
        Quaternion target = sceneRotation;
        if (open != showsOpenInScene) target = sceneRotation * Quaternion.Euler(0f, 180f, 0f);

        StopAllCoroutines();
        if (instant) transform.localRotation = target;
        else StartCoroutine(Flip(target));
    }

    IEnumerator Flip(Quaternion target)
    {
        Quaternion start = transform.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flipDuration;
            transform.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.localRotation = target;
    }
}

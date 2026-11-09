using System.Collections;
using UnityEngine;

// The object grows from 0 to its normal size.
public class GrowIn : MonoBehaviour
{
    public float duration = 0.3f;
    public bool playOnEnable = true;

    Vector3 fullScale;

    void Awake()
    {
        fullScale = transform.localScale;
    }

    void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float k = time / duration;
            k = 1f - (1f - k) * (1f - k);   // fast at first, slower at the end
            transform.localScale = fullScale * k;
            yield return null;
        }
        transform.localScale = fullScale;
    }
}

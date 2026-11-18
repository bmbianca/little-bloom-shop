using System.Collections;
using UnityEngine;

// Moves the sun (Directional Light) across the sky as orders are completed.
// No real-time clock: the day only moves forward when an order is delivered.
public class DayCycle : MonoBehaviour
{
    public Light sun;

    [Header("Sun height (degrees)")]
    public float morningAngle = 15f;
    public float sunsetAngle = 165f;

    [Header("Colors")]
    public Color morningColor = new Color(1f, 0.88f, 0.7f);
    public Color noonColor = Color.white;
    public Color sunsetColor = new Color(1f, 0.55f, 0.35f);
    public Color nightColor = new Color(0.35f, 0.4f, 0.65f);

    [Header("Brightness")]
    public float dayIntensity = 1f;
    public float nightIntensity = 0.15f;
    public float moveDuration = 3f;

    float yaw;
    float currentAngle;

    void Awake()
    {
        if (sun == null) return;
        yaw = sun.transform.eulerAngles.y;
        currentAngle = morningAngle;
    }

    // progress: 0 = morning, 1 = sunset
    public void SetDayProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        float angle = Mathf.Lerp(morningAngle, sunsetAngle, progress);

        Color color;
        if (progress < 0.5f) color = Color.Lerp(morningColor, noonColor, progress * 2f);
        else color = Color.Lerp(noonColor, sunsetColor, (progress - 0.5f) * 2f);

        MoveSun(angle, color, dayIntensity, false);
    }

    public void SetNight(bool instant)
    {
        MoveSun(sunsetAngle, nightColor, nightIntensity, instant);
    }

    void MoveSun(float angle, Color color, float intensity, bool instant)
    {
        if (sun == null) return;
        StopAllCoroutines();

        if (instant)
        {
            ApplySun(angle, color, intensity);
            return;
        }
        StartCoroutine(MoveSunSlowly(angle, color, intensity));
    }

    IEnumerator MoveSunSlowly(float targetAngle, Color targetColor, float targetIntensity)
    {
        float startAngle = currentAngle;
        Color startColor = sun.color;
        float startIntensity = sun.intensity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            float k = Mathf.SmoothStep(0f, 1f, t);
            ApplySun(Mathf.Lerp(startAngle, targetAngle, k),
                     Color.Lerp(startColor, targetColor, k),
                     Mathf.Lerp(startIntensity, targetIntensity, k));
            yield return null;
        }
    }

    void ApplySun(float angle, Color color, float intensity)
    {
        currentAngle = angle;
        sun.transform.rotation = Quaternion.Euler(angle, yaw, 0f);
        sun.color = color;
        sun.intensity = intensity;
    }
}

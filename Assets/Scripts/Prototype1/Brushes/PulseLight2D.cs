using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PulseLight2D : MonoBehaviour
{
    [SerializeField] private Light2D targetLight;

    [Header("Intensity")]
    [SerializeField, Min(0f)] private float minIntensity = 0.6f;
    [SerializeField, Min(0f)] private float maxIntensity = 2.0f;

    [Header("Radius (optional)")]
    [SerializeField] private bool pulseRadius;
    [SerializeField, Min(0f)] private float minOuterRadius = 0.8f;
    [SerializeField, Min(0f)] private float maxOuterRadius = 1.4f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float pulsesPerSecond = 1.2f;
    [SerializeField] private bool useUnscaledTime = true;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light2D>();
        }
    }

    private void Update()
    {
        if (targetLight == null)
        {
            return;
        }

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float phase = time * pulsesPerSecond * Mathf.PI * 2f;

        // 0..1
        float t = (Mathf.Sin(phase) + 1f) * 0.5f;

        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);

        if (pulseRadius)
        {
            targetLight.pointLightOuterRadius = Mathf.Lerp(minOuterRadius, maxOuterRadius, t);
        }
    }
}

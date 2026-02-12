using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class JellyWobble : MonoBehaviour
{
    [Header("Base squash")]
    public float baseSquashX = 1f;
    public float baseSquashY = 1f;

    [Header("Wobble")]
    [Tooltip("How strong the wobble effect is")]
    public float wobbleAmplitude = 0.2f;
    [Tooltip("How fast the wobble oscillates")]
    public float wobbleFrequency = 6f;
    [Tooltip("How much the player velocity affects wobble speed")]
    public float velocityInfluence = 0.25f;

    private Material _mat;
    private Vector3 _lastPos;
    private float _wobbleTime;

    void Awake()
    {
        // Get the material used by the sprite.
        // Make sure the SpriteRenderer is using a material with the SquashStretch2D shader.
        var sr = GetComponent<SpriteRenderer>();
        _mat = sr.material;
        _lastPos = transform.position;
    }

    void Update()
    {
        // Calculate player velocity (used to make wobble stronger/faster when moving)
        Vector3 vel = (transform.position - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = transform.position;
        float speed = vel.magnitude;

        // Time for the sine wave: base frequency + extra from movement speed
        _wobbleTime += Time.deltaTime * (wobbleFrequency + speed * velocityInfluence);

        // The wobble itself: sine wave scaled by amplitude
        float wobble = Mathf.Sin(_wobbleTime) * wobbleAmplitude;

        // One axis stretches while the other squashes – creates a jelly-like feel
        float squashX = baseSquashX + wobble;
        float squashY = baseSquashY - wobble;

        // Clamp to avoid extreme values
        squashX = Mathf.Max(0.3f, squashX);
        squashY = Mathf.Max(0.3f, squashY);

        // Send values to the shader
        _mat.SetFloat("_SquashAmountX", squashX);
        _mat.SetFloat("_SquashAmountY", squashY);
    }
}
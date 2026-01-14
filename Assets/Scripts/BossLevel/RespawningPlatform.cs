using System.Collections;
using UnityEngine;

public class RespawningPlatform : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 3.0f;
    [SerializeField] private bool autoRespawn = true;

    [Header("Visual Feedback")]
    [Tooltip("Prefab to spawn when broken (e.g., wood splinters). If null, platform fades out instead.")]
    [SerializeField] private GameObject crumbleEffect;
    
    [Header("Fading Animation")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 1.0f;

    private SpriteRenderer _renderer;
    private Collider2D _collider;
    private Color _originalColor;
    private bool _isBroken = false;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        if (_renderer) _originalColor = _renderer.color;
    }

    public void BreakAndRespawn()
    {
        if (_isBroken) return;
        _isBroken = true;

        // 1. Disable Physics immediately so player falls
        if (_collider) _collider.enabled = false;

        // 2. Handle Visuals
        if (crumbleEffect != null)
        {
            // Explosive break
            Instantiate(crumbleEffect, transform.position, Quaternion.identity);
            if (_renderer) _renderer.enabled = false; // Instant hide
        }
        else
        {
            // Slow fade out
            StartCoroutine(FadeRoutine(1f, 0f, fadeOutDuration));
        }

        // 3. Schedule Respawn
        if (autoRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // Wait for the dead time
        yield return new WaitForSeconds(respawnTime);

        // 4. Phase In Visuals
        if (_renderer)
        {
            _renderer.enabled = true;
            // Force alpha to 0 before starting fade in
            Color c = _originalColor;
            c.a = 0f;
            _renderer.color = c;
            
            yield return StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration));
        }

        // 5. Re-enable Physics (after fully visible)
        if (_collider) _collider.enabled = true;
        _isBroken = false;
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        if (_renderer == null) yield break;

        float elapsed = 0f;
        Color c = _originalColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Lerp Alpha
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            _renderer.color = c;
            
            yield return null;
        }

        // Ensure final value is set
        c.a = endAlpha;
        _renderer.color = c;
    }
}
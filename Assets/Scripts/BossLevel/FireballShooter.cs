using System.Collections;
using Drawing.Data;
using Drawing.Managers.Core.Managers;
using UnityEngine;

public class FireballShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float shootInterval = 4.0f;
    [SerializeField] private float fireballSpeed = 10f;

    [Header("Visuals (Shouting Face)")]
    [Tooltip("The SpriteRenderer of the Giant's head/face.")]
    [SerializeField] private SpriteRenderer bossFaceRenderer;
    [Tooltip("The sprite to show when shooting.")]
    [SerializeField] private Sprite shoutSprite;
    [Tooltip("How long to keep the shouting face before going back to normal.")]
    [SerializeField] private float shoutDuration = 1.0f;

    private float _timer;
    private Sprite _originalSprite;
    private Coroutine _faceCoroutine;
    
    [Header("Sound Settings")]
    [SerializeField] private float soundVolume = 0.4f;

    private void Awake()
    {
        // Cache the normal face so we can return to it later
        if (bossFaceRenderer != null)
        {
            _originalSprite = bossFaceRenderer.sprite;
        }
    }

    private void OnEnable()
    {
        _timer = shootInterval;
    }

    private void OnDisable()
    {
        // Safety: If this component turns off (phase change), reset the face immediately
        if (bossFaceRenderer != null && _originalSprite != null)
        {
            bossFaceRenderer.sprite = _originalSprite;
        }
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            ShootCone();
            _timer = shootInterval;
        }
    }

    private void ShootCone()
    {
        // 1. Swap Face
        if (bossFaceRenderer != null && shoutSprite != null)
        {
            if (_faceCoroutine != null) StopCoroutine(_faceCoroutine);
            _faceCoroutine = StartCoroutine(ShoutFaceRoutine());
        }

        // 2. Shoot Fireballs
        if (playerTarget == null) return;

        Vector2 direction = (playerTarget.position - mouthPoint.position).normalized;
        float[] angles = { -15f, 0f, 15f };

        foreach (float angle in angles)
        {
            // Calculate direction spread
            Vector2 spreadDir = Quaternion.Euler(0, 0, angle) * direction;
            
            GameObject ball = Instantiate(fireballPrefab, mouthPoint.position, Quaternion.identity);
            
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb)
            {
                // Move towards the player
                rb.linearVelocity = spreadDir * fireballSpeed;

                // Calculate standard angle (0 = Right)
                float rot_z = Mathf.Atan2(spreadDir.y, spreadDir.x) * Mathf.Rad2Deg;
                
                // Add 90 degrees offset because the sprite points DOWN
                // (Down is -90 from Right, so we add 90 to compensate)
                ball.transform.rotation = Quaternion.Euler(0f, 0f, rot_z + 90f);
            }
        }
        
        AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.Fireball);
    }

    private IEnumerator ShoutFaceRoutine()
    {
        // Change to shout
        bossFaceRenderer.sprite = shoutSprite;

        // Wait
        yield return new WaitForSeconds(shoutDuration);

        // Change back
        if (_originalSprite != null)
        {
            bossFaceRenderer.sprite = _originalSprite;
        }
    }
}
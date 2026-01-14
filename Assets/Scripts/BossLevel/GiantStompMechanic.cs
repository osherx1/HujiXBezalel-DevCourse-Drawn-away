using System.Collections;
using UnityEngine;
using Drawing.Managers; // For CameraShake if desired

public class GiantStompMechanic : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform levelGeometryRoot; // The Giant object to shake
    [SerializeField] private float stompInterval = 3f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 0.5f; // How far the ground moves (Unity Units)

    private float _timer;
    private Vector3 _originalPos;

    private void Start()
    {
        if (levelGeometryRoot != null) 
            _originalPos = levelGeometryRoot.position;
    }

    private void OnEnable()
    {
        _timer = stompInterval; // Start with a delay
    }

    private void OnDisable()
    {
        // Reset position when disabled so the level isn't stuck offset
        if (levelGeometryRoot != null) 
            levelGeometryRoot.position = _originalPos;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            StartCoroutine(DoStomp());
            _timer = stompInterval;
        }
    }

    private IEnumerator DoStomp()
    {
        // Optional: Play Sound
        // AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.Stomp);

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // We move the ACTUAL level object randomly
            Vector3 randomOffset = Random.insideUnitCircle * shakeIntensity;
            levelGeometryRoot.position = _originalPos + randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to normal
        levelGeometryRoot.position = _originalPos;
    }
}
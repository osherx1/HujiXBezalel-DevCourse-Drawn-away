using System.Collections;
using System.Collections.Generic;
using Drawing.Data;
using UnityEngine;
using Drawing.Managers;
using Drawing.Managers.Core.Managers; // For camera shake if you have it

public class GiantStompMechanic : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How high the leg lifts before stomping.")]
    [SerializeField] private float liftHeight = 2.0f;
    [Tooltip("Total duration of one stomp (Lift -> Slam).")]
    [SerializeField] private float animationDuration = 1.0f;
    [Tooltip("Curve for the motion. Recommended: Bell curve (Start 0, Middle 1, End 0).")]
    [SerializeField] private AnimationCurve stompCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(0.5f, 1f), 
        new Keyframe(1f, 0f)
    );

    [Header("Impact Settings")]
    [SerializeField] private float stompInterval = 3f;
    [SerializeField] private float cameraShakeDuration = 0.5f;
    [SerializeField] private float cameraShakeIntensity = 0.5f;
    [SerializeField] private Transform levelGeometryRoot; // For the shake effect

    [Header("Leg Groups")]
    [SerializeField] private LegGroup leftLeg;
    [SerializeField] private LegGroup rightLeg;
    
    [Header("Sound Settings")]
    [SerializeField] private float impactSoundVolume = 0.2f;
    [SerializeField] private float timeToPlaySound = 0.3f;
    private bool _playedSound = false;

    [System.Serializable]
    public struct LegGroup
    {
        [Tooltip("The visual sprite of the leg.")]
        public Transform legVisual;
        [Tooltip("The parent object holding all platforms attached to this leg.")]
        public Transform platformRoot;
        [HideInInspector] public Vector3 legStartPos;
        [HideInInspector] public Vector3 platformStartPos;
    }

    private float _timer;
    private bool _isLeftLegNext = true; // Toggle for alternating
    private Vector3 _levelOriginalPos;

    private void Start()
    {
        // Cache initial positions so we don't drift over time
        CachePositions(ref leftLeg);
        CachePositions(ref rightLeg);

        if (levelGeometryRoot != null) 
            _levelOriginalPos = levelGeometryRoot.position;
    }

    private void CachePositions(ref LegGroup group)
    {
        if (group.legVisual != null) group.legStartPos = group.legVisual.position;
        if (group.platformRoot != null) group.platformStartPos = group.platformRoot.position;
    }

    private void OnEnable()
    {
        _timer = stompInterval;
    }

    private void OnDisable()
    {
        // Reset everything to prevent getting stuck in mid-air
        ResetLeg(leftLeg);
        ResetLeg(rightLeg);
        if (levelGeometryRoot != null) levelGeometryRoot.position = _levelOriginalPos;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            // Pick the leg
            LegGroup activeLeg = _isLeftLegNext ? leftLeg : rightLeg;
            
            StartCoroutine(StompRoutine(activeLeg));
            
            // Flip for next time
            _isLeftLegNext = !_isLeftLegNext;
            _timer = stompInterval;
        }
    }

    private IEnumerator StompRoutine(LegGroup leg)
    {
        float t = 0f;

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            float progress = t / animationDuration;
            
            // Evaluate curve (0 to 1 to 0)
            float heightFactor = stompCurve.Evaluate(progress);
            float currentYOffset = heightFactor * liftHeight;

            // Apply to Leg Visual
            if (leg.legVisual != null)
            {
                leg.legVisual.position = leg.legStartPos + Vector3.up * currentYOffset;
            }

            // Apply to Platforms
            if (leg.platformRoot != null)
            {
                leg.platformRoot.position = leg.platformStartPos + Vector3.up * currentYOffset;
            }

            if (progress >= timeToPlaySound && !_playedSound)
            {
                AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.GiantStomp, impactSoundVolume);
                _playedSound = true;
            }

            yield return null;
        }
        
        _playedSound = false;

        // Ensure perfect return to start
        ResetLeg(leg);

        // TRIGGER IMPACT (Shake)
        StartCoroutine(ShakeLevel());
    }

    private void ResetLeg(LegGroup leg)
    {
        if (leg.legVisual != null) leg.legVisual.position = leg.legStartPos;
        if (leg.platformRoot != null) leg.platformRoot.position = leg.platformStartPos;
    }

    private IEnumerator ShakeLevel()
    {
        // Play Sound here if desired
        // AudioManager.Instance.PlaySound...

        if (levelGeometryRoot == null) yield break;

        float elapsed = 0f;
        while (elapsed < cameraShakeDuration)
        {
            Vector3 randomOffset = Random.insideUnitCircle * cameraShakeIntensity;
            levelGeometryRoot.position = _levelOriginalPos + randomOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        levelGeometryRoot.position = _levelOriginalPos;
    }
}
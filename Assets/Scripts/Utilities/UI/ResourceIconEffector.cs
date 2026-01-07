using UnityEngine;
using UnityEngine.UI;

namespace Utilities.UI
{
    [System.Serializable]
    public class ResourceIconEffector
    {
        // --- Enums ---
        public enum ContinuousEffect { None, Rumble, Wobble, Spin }
        public enum ImpactType { None, PulseScale, Shake, FlashColor, PulseAndFlash, ShakeAndFlash }
        private enum ImpactDirection { None, Gain, Loss }

        // --- Configuration Struct ---
        [System.Serializable]
        public struct ImpactSettings
        {
            public ImpactType Type;
            public float Duration;
            public Color TintColor;
            [Tooltip("For Pulse: Scale Multiplier (e.g. 1.5). For Shake: Intensity.")]
            public float Intensity; 
        }

        [Header("Target")]
        [SerializeField] private Image iconImage;

        [Header("Continuous Motion (While Bar Fills)")]
        [SerializeField] private ContinuousEffect continuousEffect = ContinuousEffect.Rumble;
        [SerializeField] private float continuousSpeed = 20f;
        [SerializeField] private float continuousStrength = 5f;

        [Header("Directional Feedback")]
        [SerializeField] private ImpactSettings gainSettings = new ImpactSettings 
        { 
            Type = ImpactType.PulseAndFlash, 
            Duration = 0.3f, 
            TintColor = Color.green, 
            Intensity = 1.4f 
        };

        [SerializeField] private ImpactSettings lossSettings = new ImpactSettings 
        { 
            Type = ImpactType.ShakeAndFlash, 
            Duration = 0.4f, 
            TintColor = Color.red, 
            Intensity = 15f 
        };

        // --- State Cache ---
        private RectTransform _rect;
        private Vector3 _defaultScale;
        private Quaternion _defaultRotation;
        private Color _defaultColor;
        
        // --- Runtime State ---
        private float _impactTimer;
        private bool _isImpacting;
        private ImpactDirection _currentDirection;
        
        // Helper to access the currently active settings based on direction
        private ImpactSettings CurrentSettings => _currentDirection == ImpactDirection.Gain ? gainSettings : lossSettings;

        public void Initialize()
        {
            if (iconImage == null) return;

            // Cache defaults
            _rect = iconImage.rectTransform;
            _defaultScale = _rect.localScale;
            _defaultRotation = _rect.localRotation;
            _defaultColor = iconImage.color;
        }

        /// <summary>
        /// Triggers an effect based on the direction of the resource change.
        /// </summary>
        /// <param name="amount">Positive for Gain, Negative for Loss.</param>
        public void TriggerImpact(int amount)
        {
            if (iconImage == null || !iconImage.enabled) return;
            if (amount == 0) return;

            // determine direction
            var newDirection = amount > 0 ? ImpactDirection.Gain : ImpactDirection.Loss;
            
            // Check if we should ignore this trigger (e.g. if the effect type is None)
            ImpactSettings potentialSettings = newDirection == ImpactDirection.Gain ? gainSettings : lossSettings;
            if (potentialSettings.Type == ImpactType.None) return;

            // Reset timer and set state
            _currentDirection = newDirection;
            _impactTimer = 0f;
            _isImpacting = true;
        }

        /// <summary>
        /// Main tick loop. Calculates math for current frame.
        /// </summary>
        public bool Process(bool isBarMoving)
        {
            if (iconImage == null || !iconImage.enabled) return false;

            bool isBusy = false;
            
            // Reset to defaults at start of frame to ensure clean layering
            // Note: In high-perf scenarios, we might check diffs before setting, 
            // but setting local properties is relatively cheap compared to allocations.
            _rect.localScale = _defaultScale;
            iconImage.color = _defaultColor;
            
            // We calculate the base rotation first
            Quaternion targetRotation = _defaultRotation;

            // 1. Calculate Continuous Effect (Base Layer)
            if (isBarMoving && continuousEffect != ContinuousEffect.None)
            {
                targetRotation = CalculateContinuousRotation();
                isBusy = true;
            }

            // 2. Calculate Impact Effect (Overlay Layer)
            if (_isImpacting)
            {
                // Proceed timer
                _impactTimer += Time.deltaTime;
                ImpactSettings settings = CurrentSettings;
                
                float t = _impactTimer / settings.Duration;

                if (t >= 1.0f)
                {
                    _isImpacting = false;
                    _currentDirection = ImpactDirection.None;
                }
                else
                {
                    ApplyImpactMath(t, settings, ref targetRotation);
                    isBusy = true;
                }
            }

            // Apply final rotation (Combined Continuous + Impact)
            _rect.localRotation = targetRotation;

            return isBusy;
        }

        private Quaternion CalculateContinuousRotation()
        {
            float angle = 0f;
            switch (continuousEffect)
            {
                case ContinuousEffect.Rumble:
                    float noise = (Mathf.PerlinNoise(Time.time * continuousSpeed, 0f) - 0.5f) * 2f;
                    angle = noise * continuousStrength;
                    break;
                case ContinuousEffect.Wobble:
                    angle = Mathf.Sin(Time.time * continuousSpeed) * continuousStrength;
                    break;
                case ContinuousEffect.Spin:
                    // For spin, we just return the calculation directly
                    return _rect.localRotation * Quaternion.Euler(0, 0, -continuousSpeed * Time.deltaTime);
            }
            return Quaternion.Euler(0, 0, angle);
        }

        private void ApplyImpactMath(float t, ImpactSettings settings, ref Quaternion rotationRef)
        {
            // --- Color Logic ---
            if (settings.Type == ImpactType.FlashColor || 
                settings.Type == ImpactType.PulseAndFlash || 
                settings.Type == ImpactType.ShakeAndFlash)
            {
                // Flash to color then fade back to default
                // Using t^2 makes the color fade out faster, keeping impact snappy
                iconImage.color = Color.Lerp(settings.TintColor, _defaultColor, t * t);
            }

            // --- Scale Logic ---
            if (settings.Type == ImpactType.PulseScale || 
                settings.Type == ImpactType.PulseAndFlash)
            {
                // Sine wave 0->1->0
                float pulse = Mathf.Sin(t * Mathf.PI);
                _rect.localScale = Vector3.LerpUnclamped(_defaultScale, _defaultScale * settings.Intensity, pulse);
            }

            // --- Shake/Rotation Logic ---
            if (settings.Type == ImpactType.Shake || 
                settings.Type == ImpactType.ShakeAndFlash)
            {
                // Violent shake that creates a distinct override to the continuous wobble
                // We use a high frequency noise based on the timer, not global time, so every hit feels identical
                float decay = 1f - t; // Shake gets weaker over time
                float noise = (Mathf.PerlinNoise(_impactTimer * 50f, 10f) - 0.5f) * 2f;
                float shakeAngle = noise * settings.Intensity * decay;
                
                // Override the reference rotation completely for shake impact
                rotationRef = Quaternion.Euler(0, 0, shakeAngle);
            }
        }

        public void ForceReset()
        {
            if (iconImage == null || _rect == null) return;
            
            _rect.localScale = _defaultScale;
            _rect.localRotation = _defaultRotation;
            iconImage.color = _defaultColor;
            _isImpacting = false;
            _currentDirection = ImpactDirection.None;
        }
    }
}
/*using UnityEngine;
using UnityEngine.UI;

namespace Utilities.UI
{
    [System.Serializable]
    public class ResourceIconEffector
    {
        public enum ContinuousEffect { None, Rumble, Wobble, Spin }
        public enum ImpactEffect { None, PulseScale, FlashColor }

        [Header("Target")]
        [SerializeField] private Image iconImage;

        [Header("Configuration")]
        [SerializeField] private ContinuousEffect activeEffect = ContinuousEffect.Rumble;
        [SerializeField] private ImpactEffect triggerEffect = ImpactEffect.PulseScale;

        [Header("Settings - Rumble/Wobble")]
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private float shakeSpeed = 20f;

        [Header("Settings - Spin")]
        [SerializeField] private float spinSpeed = 360f;

        [Header("Settings - Pulse/Flash")]
        [SerializeField] private float impactDuration = 0.2f;
        [SerializeField] private float pulseScaleMultiplier = 1.3f;
        [SerializeField] private Color flashColor = Color.white;

        // --- State Cache ---
        private RectTransform _rect;
        private Vector3 _defaultScale;
        private Quaternion _defaultRotation;
        private Color _defaultColor;
        
        // --- Runtime State ---
        private float _impactTimer;
        private bool _isImpacting;
        public bool IsImpacting => _isImpacting;

        /// <summary>
        /// Called by the parent Awake to cache defaults.
        /// </summary>
        public void Initialize()
        {
            if (iconImage == null) return;
            bool enable = iconImage.enabled;
            
            iconImage.enabled = true; // Temporarily enable to get RectTransform
            _rect = iconImage.rectTransform;
            _defaultScale = _rect.localScale;
            _defaultRotation = _rect.localRotation;
            _defaultColor = iconImage.color;
            iconImage.enabled = enable; // Restore original state
            
        }

        /// <summary>
        /// Call this when the resource value changes to trigger "One Shot" effects.
        /// </summary>
        public void TriggerImpact()
        {
            if (iconImage == null || !iconImage.enabled) return;
            if (triggerEffect == ImpactEffect.None) return;

            _impactTimer = 0f;
            _isImpacting = true;
        }

        /// <summary>
        /// The main tick loop. Returns TRUE if the icon is currently visually active (animating).
        /// </summary>
        public bool Process(bool isBarMoving)
        {
            if (iconImage == null || !iconImage.enabled) return false;

            bool isBusy = false;

            // 1. Handle Continuous Effects (Only when bar is moving)
            if (isBarMoving)
            {
                ApplyContinuousEffect();
                isBusy = true;
            }
            else
            {
                ResetContinuousState();
            }

            // 2. Handle Impact Effects (One-shot animation)
            if (_isImpacting)
            {
                ApplyImpactEffect();
                isBusy = true;
            }

            return isBusy;
        }

        private void ApplyContinuousEffect()
        {
            switch (activeEffect)
            {
                case ContinuousEffect.Rumble:
                    // Perlin noise for smooth chaotic shaking
                    float noise = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f;
                    _rect.localRotation = Quaternion.Euler(0, 0, noise * shakeStrength);
                    break;

                case ContinuousEffect.Wobble:
                    // Sine wave for rhythmic rocking
                    float sine = Mathf.Sin(Time.time * shakeSpeed);
                    _rect.localRotation = Quaternion.Euler(0, 0, sine * shakeStrength);
                    break;

                case ContinuousEffect.Spin:
                    // Continuous rotation
                    _rect.Rotate(0, 0, -spinSpeed * Time.deltaTime);
                    break;
            }
        }

        private void ResetContinuousState()
        {
            // Snap back to zero if we aren't running an effect
            // We check the distinct comparison to avoid setting rect properties every frame (GC/Perf)
            if (activeEffect != ContinuousEffect.None && _rect.localRotation != _defaultRotation)
            {
                _rect.localRotation = _defaultRotation;
            }
        }

        private void ApplyImpactEffect()
        {
            _impactTimer += Time.deltaTime;
            float t = _impactTimer / impactDuration;

            if (t >= 1.0f)
            {
                // Finished
                _rect.localScale = _defaultScale;
                iconImage.color = _defaultColor;
                _isImpacting = false;
                return;
            }

            switch (triggerEffect)
            {
                case ImpactEffect.PulseScale:
                    // 0 -> 1 -> 0 Sine wave
                    float pulse = Mathf.Sin(t * Mathf.PI);
                    _rect.localScale = Vector3.LerpUnclamped(_defaultScale, _defaultScale * pulseScaleMultiplier, pulse);
                    break;

                case ImpactEffect.FlashColor:
                    // Flash color -> Default color
                    iconImage.color = Color.Lerp(flashColor, _defaultColor, t);
                    break;
            }
        }
        
        // Helper to force reset if parent is disabled
        public void ForceReset()
        {
            if (iconImage == null) return;
            
            // FIX: Prevent crash in OnValidate/Editor when Awake hasn't run yet
            if (_rect == null) return;

            _rect.localScale = _defaultScale;
            _rect.localRotation = _defaultRotation;
            iconImage.color = _defaultColor;
            _isImpacting = false;
        }
    }
}*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Utilities.UI
{
    public class ResourceBarTracker : MonoBehaviour
    {
        #region Configuration Data

        [System.Serializable]
        public struct ResourceSettings
        {
            public int Current;
            public int Max;
            public int AbsoluteMax;
            public bool OverkillPossible;
        }

        #endregion

        [Header("References")] [SerializeField]
        private Image bar;

        [SerializeField] private TMP_Text resourceValueTextField;

        [Header("Icon Visuals")] [SerializeField]
        private ResourceIconEffector iconEffect;

        // --- NEW: Zero State Configuration ---
        [Header("Zero State Visuals")] [Tooltip("The icon image to change color when value is 0")] [SerializeField]
        private Image iconToColor;

        [Tooltip("The color the icon changes to when value reaches 0")] [SerializeField]
        private Color emptyColor = Color.red;

        private Color _originalIconColor = Color.white; // To store the normal color
        // -------------------------------------

        [Header("Resource Data")] [SerializeField]
        private ResourceSettings resourceData = new ResourceSettings { Current = 100, Max = 100, AbsoluteMax = 1000 };

        [Header("Bar Visuals")] [SerializeField]
        private ShapeType shapeOfBar;

        [SerializeField] private DisplayType valueDisplayMode = DisplayType.Percentage;

        [Tooltip("Time to reach target value (approx). Lower is faster.")] [SerializeField, Range(0, 0.5f)]
        private float smoothTime = 0.15f;

        [SerializeField] private bool fillAnimationOnStart = true;

        [Header("Arc Settings")] [SerializeField, Range(0, 360)]
        private int endDegreeValue = 360;

        [Header("Gradient Settings")] [SerializeField]
        private bool useGradient;

        [SerializeField] private Gradient barGradient;

        [Header("Events")] [SerializeField] private UnityEvent barIsFilledUp;

        // --- State Tracking ---
        private float _visualFillAmount; // Where the bar currently IS
        private float _targetFillAmount; // Where the bar WANTS to be
        private float _currentVelocity; // For SmoothDamp
        private bool _isInitialized;
        private bool _animate; // Main gate for Update loop


        // --- Enums ---
        public enum ShapeType
        {
            [InspectorName("Rectangle (Horizontal)")]
            RectangleHorizontal,

            [InspectorName("Rectangle (Vertical)")]
            RectangleVertical,
            Circle,
            Arc
        }

        public enum DisplayType
        {
            [InspectorName("Long (50|100)")] LongValue,
            [InspectorName("Short (50)")] ShortValue,
            [InspectorName("Percent (85%)")] Percentage,
            None
        }

        #region Unity Lifecycle

        private void OnValidate()
        {
            // Editor-time preview
            if (bar != null) ConfigureBarShape();
            SnapToTarget();
        }

        private void Awake()
        {
            if (bar == null)
            {
                Debug.LogError($"[ResourceBarTracker] Missing Image reference on {name}", this);
                enabled = false;
                return;
            }

            // --- NEW: Capture original color ---
            if (iconToColor != null)
            {
                _originalIconColor = iconToColor.color;
            }
            // -----------------------------------

            ConfigureBarShape();

            // Initialize the separate Icon Effector logic
            iconEffect.Initialize();

            // Initial Setup
            _targetFillAmount = CalculateTargetFill();

            if (fillAnimationOnStart)
            {
                _visualFillAmount = 0f; // Start empty and animate up
                _animate = true;
            }
            else
            {
                SnapToTarget();
            }

            // Check initial color state
            HandleZeroStateColor();

            _isInitialized = true;
        }

        private void Update()
        {
            // Optimization: Global gate to stop processing completely if nothing is happening
            if (!_animate || !_isInitialized) return;

            // 1. Process Bar Logic (Returns true if bar is still moving)
            bool isBarMoving = ProcessBarAnimation();

            // 2. Delegate Visuals to the Effector Class
            // We pass 'isBarMoving' so the icon knows if it should rumble
            bool isIconBusy = iconEffect.Process(isBarMoving);

            // 3. Sleep only if BOTH systems are settled
            if (!isBarMoving && !isIconBusy)
            {
                _animate = false;
            }
        }

        private void OnDisable()
        {
            // Safety: Reset icon if the bar is hidden while shaking
            iconEffect.ForceReset();
            /*
            HandleZeroStateColor();
        */
        }

        #endregion

        #region Animation Logic

        private bool ProcessBarAnimation()
        {
            // If bar is disabled or hidden, skip calculation but treat as "done"
            if (!bar.enabled || Mathf.Abs(_visualFillAmount - _targetFillAmount) < 0.001f)
            {
                if (Mathf.Abs(_visualFillAmount - _targetFillAmount) >= 0.001f)
                {
                    // Snap only if we are finishing the animation
                    SnapToTarget();
                    CheckFilledEvent();
                }

                return false;
            }

            // Move Visual towards Target
            _visualFillAmount = Mathf.SmoothDamp(
                _visualFillAmount,
                _targetFillAmount,
                ref _currentVelocity,
                smoothTime
            );

            // Apply to UI
            bar.fillAmount = _visualFillAmount;
            ApplyGradient(_visualFillAmount);

            return true;
        }

        #endregion

        #region Public API

        public bool ChangeResourceByAmount(int amount, bool animate = true)
        {
            if (!resourceData.OverkillPossible && resourceData.Current + amount < 0) return false;

            // Update Data
            resourceData.Current = Mathf.Clamp(resourceData.Current + amount, 0, resourceData.Max);

            // Calculate new visual target
            _targetFillAmount = CalculateTargetFill();
            UpdateText();

            // --- NEW: Check Color State ---
            HandleZeroStateColor();
            // ------------------------------

            // Handle Animation Triggers
            if (Application.isPlaying && animate)
            {
                iconEffect.TriggerImpact(amount); // Fire the "Hit" effect (Flash/Pulse)
                _animate = true; // Wake up Update loop (Rumble/Fill)
            }
            else
            {
                SnapToTarget();
            }

            return true;
        }

        public void ChangeMaxAmountTo(int newMax, bool animate = true)
        {
            resourceData.Max = Mathf.Clamp(newMax, 0, resourceData.AbsoluteMax);
            resourceData.Current = Mathf.Clamp(resourceData.Current, 0, resourceData.Max);

            _targetFillAmount = CalculateTargetFill();
            UpdateText();
            HandleZeroStateColor(); // Update color check

            if (Application.isPlaying && animate)
            {
                iconEffect.TriggerImpact(newMax);
                _animate = true;
            }
            else
            {
                SnapToTarget();
            }
        }

        public void SetBarVisibility(bool isVisible)
        {
            if (bar != null && bar.enabled != isVisible)
            {
                
                if (!isVisible)
                {
                    // Force complete bar animation if hiding
                    if (Mathf.Abs(_visualFillAmount - _targetFillAmount) > 0.01f)
                    {
                        SnapToTarget();
                    }

                    _animate = false;
                }

                bar.enabled = isVisible;
                HandleZeroStateColor();
            }
        }

        // --- Setup / Reset Wrappers ---

        public void Setup(int current, int max, int absMax, bool overkill, ShapeType shape, float speed,
            DisplayType display, bool useGrad, Gradient grad)
        {
            resourceData = new ResourceSettings
                { Current = current, Max = max, AbsoluteMax = absMax, OverkillPossible = overkill };
            shapeOfBar = shape;
            smoothTime = speed;
            valueDisplayMode = display;
            useGradient = useGrad;
            barGradient = grad;

            ConfigureBarShape();
            ChangeResourceByAmount(0, true);
        }

        public void ResetWithoutAnimation(int current, int max, int absMax)
        {
            resourceData.Current = current;
            resourceData.Max = max;
            resourceData.AbsoluteMax = absMax;

            _targetFillAmount = CalculateTargetFill();
            SnapToTarget();
            HandleZeroStateColor(); // Update color check
        }

        #endregion

        #region Internal Logic

        // --- NEW: Logic to handle Icon Color ---
        private void HandleZeroStateColor()
        {
            if (iconToColor == null) return;


            if (bar.enabled && resourceData.Current <= 0)
            {
                iconToColor.enabled = true;
                iconToColor.color = emptyColor;
            }
            else
            {
                iconToColor.color = _originalIconColor;
                iconToColor.enabled = false;
            }
        }
        // ---------------------------------------

        private void ConfigureBarShape()
        {
            if (bar == null) return;

            switch (shapeOfBar)
            {
                case ShapeType.RectangleHorizontal: bar.fillMethod = Image.FillMethod.Horizontal; break;
                case ShapeType.RectangleVertical: bar.fillMethod = Image.FillMethod.Vertical; break;
                case ShapeType.Circle:
                case ShapeType.Arc: bar.fillMethod = Image.FillMethod.Radial360; break;
            }
        }

        private float CalculateTargetFill()
        {
            if (resourceData.Max <= 0) return 0f;

            float ratio = (float)resourceData.Current / resourceData.Max;

            if (shapeOfBar == ShapeType.Arc)
            {
                // Normalize ratio to the arc size
                return ratio * (endDegreeValue / 360f);
            }

            return ratio;
        }

        private void SnapToTarget()
        {
            if (bar == null) return;

            _targetFillAmount = CalculateTargetFill();

            if (bar.enabled)
            {
                _visualFillAmount = _targetFillAmount;
                bar.fillAmount = _visualFillAmount;
                ApplyGradient(_visualFillAmount);
                UpdateText();
                iconEffect.ForceReset(); // Reset icon if we snap
                HandleZeroStateColor(); // Ensure color is correct on snap
            }
        }

        private void ApplyGradient(float currentFill)
        {
            if (!useGradient || barGradient == null)
            {
                if (bar.color != Color.white) bar.color = Color.white;
                return;
            }

            // Normalization for Arc
            float evalTime = currentFill;
            if (shapeOfBar == ShapeType.Arc && endDegreeValue > 0)
            {
                evalTime = currentFill / (endDegreeValue / 360f);
            }

            bar.color = barGradient.Evaluate(evalTime);
        }

        private void UpdateText()
        {
            if (resourceValueTextField == null) return;

            switch (valueDisplayMode)
            {
                case DisplayType.LongValue:
                    resourceValueTextField.SetText("{0}/{1}", resourceData.Current, resourceData.Max);
                    break;
                case DisplayType.ShortValue:
                    resourceValueTextField.SetText("{0}", resourceData.Current);
                    break;
                case DisplayType.Percentage:
                    float percent = resourceData.Max > 0 ? ((float)resourceData.Current / resourceData.Max) * 100f : 0f;
                    resourceValueTextField.SetText("{0:0} %", percent);
                    break;
                case DisplayType.None:
                    resourceValueTextField.SetText(string.Empty);
                    break;
            }
        }

        private void CheckFilledEvent()
        {
            if (_visualFillAmount >= 0.999f)
            {
                barIsFilledUp?.Invoke();
            }
        }

        #endregion

        #region Tests

        [ContextMenu("Test -10")]
        public void TestSub() => ChangeResourceByAmount(-10);

        [ContextMenu("Test +10")]
        public void TestAdd() => ChangeResourceByAmount(10);

        #endregion
    }
}


/*using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Utilities.UI
{
    public class ResourceBarTracker : MonoBehaviour
    {
        #region Configuration Data

        [System.Serializable]
        public struct ResourceSettings
        {
            public int Current;
            public int Max;
            public int AbsoluteMax;
            public bool OverkillPossible;
        }

        #endregion

        [Header("References")]
        [SerializeField] private Image bar;
        [SerializeField] private TMP_Text resourceValueTextField;

        [Header("Icon Visuals")]
        [SerializeField] private ResourceIconEffector iconEffect;

        [Header("Resource Data")]
        [SerializeField] private ResourceSettings resourceData = new ResourceSettings { Current = 100, Max = 100, AbsoluteMax = 1000 };

        [Header("Bar Visuals")]
        [SerializeField] private ShapeType shapeOfBar;
        [SerializeField] private DisplayType valueDisplayMode = DisplayType.Percentage;

        [Tooltip("Time to reach target value (approx). Lower is faster.")]
        [SerializeField, Range(0, 0.5f)] private float smoothTime = 0.15f;
        [SerializeField] private bool fillAnimationOnStart = true;

        [Header("Arc Settings")]
        [SerializeField, Range(0, 360)] private int endDegreeValue = 360;

        [Header("Gradient Settings")]
        [SerializeField] private bool useGradient;
        [SerializeField] private Gradient barGradient;

        [Header("Events")]
        [SerializeField] private UnityEvent barIsFilledUp;

        // --- State Tracking ---
        private float _visualFillAmount; // Where the bar currently IS
        private float _targetFillAmount; // Where the bar WANTS to be
        private float _currentVelocity; // For SmoothDamp
        private bool _isInitialized;
        private bool _animate; // Main gate for Update loop


        // --- Enums ---
        public enum ShapeType
        {
            [InspectorName("Rectangle (Horizontal)")] RectangleHorizontal,
            [InspectorName("Rectangle (Vertical)")] RectangleVertical,
            Circle,
            Arc
        }

        public enum DisplayType
        {
            [InspectorName("Long (50|100)")] LongValue,
            [InspectorName("Short (50)")] ShortValue,
            [InspectorName("Percent (85%)")] Percentage,
            None
        }

        #region Unity Lifecycle

        private void OnValidate()
        {
            // Editor-time preview
            if (bar != null) ConfigureBarShape();
            SnapToTarget();
        }

        private void Awake()
        {
            if (bar == null)
            {
                Debug.LogError($"[ResourceBarTracker] Missing Image reference on {name}", this);
                enabled = false;
                return;
            }

            ConfigureBarShape();

            // Initialize the separate Icon Effector logic
            iconEffect.Initialize();

            // Initial Setup
            _targetFillAmount = CalculateTargetFill();

            if (fillAnimationOnStart)
            {
                _visualFillAmount = 0f; // Start empty and animate up
                _animate = true;
            }
            else
            {
                SnapToTarget();
            }

            _isInitialized = true;
        }

        private void Update()
        {
            // Optimization: Global gate to stop processing completely if nothing is happening
            if (!_animate || !_isInitialized) return;

            // 1. Process Bar Logic (Returns true if bar is still moving)
            bool isBarMoving = ProcessBarAnimation();

            // 2. Delegate Visuals to the Effector Class
            // We pass 'isBarMoving' so the icon knows if it should rumble
            bool isIconBusy = iconEffect.Process(isBarMoving);

            // 3. Sleep only if BOTH systems are settled
            if (!isBarMoving && !isIconBusy)
            {
                _animate = false;
            }
        }

        private void OnDisable()
        {
            // Safety: Reset icon if the bar is hidden while shaking
            iconEffect.ForceReset();
        }

        #endregion

        #region Animation Logic

        private bool ProcessBarAnimation()
        {
            // If bar is disabled or hidden, skip calculation but treat as "done"
            if (!bar.enabled || Mathf.Abs(_visualFillAmount - _targetFillAmount) < 0.001f)
            {
                if (Mathf.Abs(_visualFillAmount - _targetFillAmount) >= 0.001f)
                {
                    // Snap only if we are finishing the animation
                    SnapToTarget();
                    CheckFilledEvent();
                }
                return false;
            }

            // Move Visual towards Target
            _visualFillAmount = Mathf.SmoothDamp(
                _visualFillAmount,
                _targetFillAmount,
                ref _currentVelocity,
                smoothTime
            );

            // Apply to UI
            bar.fillAmount = _visualFillAmount;
            ApplyGradient(_visualFillAmount);

            return true;
        }

        #endregion

        #region Public API

        public bool ChangeResourceByAmount(int amount, bool animate = true)
        {
            if (!resourceData.OverkillPossible && resourceData.Current + amount < 0) return false;

            // Update Data
            resourceData.Current = Mathf.Clamp(resourceData.Current + amount, 0, resourceData.Max);

            // Calculate new visual target
            _targetFillAmount = CalculateTargetFill();
            UpdateText();

            // Handle Animation Triggers
            if (Application.isPlaying && animate)
            {
                iconEffect.TriggerImpact(amount); // Fire the "Hit" effect (Flash/Pulse)
                _animate = true;            // Wake up Update loop (Rumble/Fill)
            }
            else
            {
                SnapToTarget();
            }

            return true;
        }

        public void ChangeMaxAmountTo(int newMax, bool animate = true)
        {
            resourceData.Max = Mathf.Clamp(newMax, 0, resourceData.AbsoluteMax);
            resourceData.Current = Mathf.Clamp(resourceData.Current, 0, resourceData.Max);

            _targetFillAmount = CalculateTargetFill();
            UpdateText();

            if (Application.isPlaying && animate)
            {
                iconEffect.TriggerImpact(newMax);
                _animate = true;
            }
            else
            {
                SnapToTarget();
            }
        }

        public void SetBarVisibility(bool isVisible)
        {
            if (bar != null && bar.enabled != isVisible)
            {
                if (!isVisible)
                {
                    // Force complete bar animation if hiding
                    if (Mathf.Abs(_visualFillAmount - _targetFillAmount) > 0.01f)
                    {
                        SnapToTarget();
                    }
                    _animate = false;
                }
                bar.enabled = isVisible;
            }
        }

        // --- Setup / Reset Wrappers ---

        public void Setup(int current, int max, int absMax, bool overkill, ShapeType shape, float speed,
            DisplayType display, bool useGrad, Gradient grad)
        {
            resourceData = new ResourceSettings
                { Current = current, Max = max, AbsoluteMax = absMax, OverkillPossible = overkill };
            shapeOfBar = shape;
            smoothTime = speed;
            valueDisplayMode = display;
            useGradient = useGrad;
            barGradient = grad;

            ConfigureBarShape();
            ChangeResourceByAmount(0, true);
        }

        public void ResetWithoutAnimation(int current, int max, int absMax)
        {
            resourceData.Current = current;
            resourceData.Max = max;
            resourceData.AbsoluteMax = absMax;

            _targetFillAmount = CalculateTargetFill();
            SnapToTarget();
        }

        #endregion

        #region Internal Logic

        private void ConfigureBarShape()
        {
            if (bar == null) return;

            switch (shapeOfBar)
            {
                case ShapeType.RectangleHorizontal: bar.fillMethod = Image.FillMethod.Horizontal; break;
                case ShapeType.RectangleVertical: bar.fillMethod = Image.FillMethod.Vertical; break;
                case ShapeType.Circle:
                case ShapeType.Arc: bar.fillMethod = Image.FillMethod.Radial360; break;
            }
        }

        private float CalculateTargetFill()
        {
            if (resourceData.Max <= 0) return 0f;

            float ratio = (float)resourceData.Current / resourceData.Max;

            if (shapeOfBar == ShapeType.Arc)
            {
                // Normalize ratio to the arc size
                return ratio * (endDegreeValue / 360f);
            }

            return ratio;
        }

        private void SnapToTarget()
        {
            if (bar == null) return;

            _targetFillAmount = CalculateTargetFill();

            if (bar.enabled)
            {
                _visualFillAmount = _targetFillAmount;
                bar.fillAmount = _visualFillAmount;
                ApplyGradient(_visualFillAmount);
                UpdateText();
                iconEffect.ForceReset(); // Reset icon if we snap
            }
        }

        private void ApplyGradient(float currentFill)
        {
            if (!useGradient || barGradient == null)
            {
                if (bar.color != Color.white) bar.color = Color.white;
                return;
            }

            // Normalization for Arc
            float evalTime = currentFill;
            if (shapeOfBar == ShapeType.Arc && endDegreeValue > 0)
            {
                evalTime = currentFill / (endDegreeValue / 360f);
            }

            bar.color = barGradient.Evaluate(evalTime);
        }

        private void UpdateText()
        {
            if (resourceValueTextField == null) return;

            switch (valueDisplayMode)
            {
                case DisplayType.LongValue:
                    resourceValueTextField.SetText("{0}/{1}", resourceData.Current, resourceData.Max);
                    break;
                case DisplayType.ShortValue:
                    resourceValueTextField.SetText("{0}", resourceData.Current);
                    break;
                case DisplayType.Percentage:
                    float percent = resourceData.Max > 0 ? ((float)resourceData.Current / resourceData.Max) * 100f : 0f;
                    resourceValueTextField.SetText("{0:0} %", percent);
                    break;
                case DisplayType.None:
                    resourceValueTextField.SetText(string.Empty);
                    break;
            }
        }

        private void CheckFilledEvent()
        {
            if (_visualFillAmount >= 0.999f)
            {
                barIsFilledUp?.Invoke();
            }
        }

        #endregion

        #region Tests

        [ContextMenu("Test -10")]
        public void TestSub() => ChangeResourceByAmount(-10);

        [ContextMenu("Test +10")]
        public void TestAdd() => ChangeResourceByAmount(10);

        #endregion
    }
}*/


/*
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Utilities.UI
{
    public class ResourceBarTracker : MonoBehaviour
    {
        #region Configuration Data

        [System.Serializable]
        public struct ResourceSettings
        {
            public int Current;
            public int Max;
            public int AbsoluteMax;
            public bool OverkillPossible;
        }

        #endregion

        [Header("References")]
        [SerializeField] private Image bar;
        [SerializeField] private TMP_Text resourceValueTextField;

        // --- NEW: Icon Reference ---
        [SerializeField] private Image resourceIcon;

        [Header("Resource Data")]
        [SerializeField] private ResourceSettings resourceData = new ResourceSettings { Current = 100, Max = 100, AbsoluteMax = 1000 };

        [Header("Visual Settings (Bar)")]
        [SerializeField] private ShapeType shapeOfBar;
        [SerializeField] private DisplayType valueDisplayMode = DisplayType.Percentage;
        [Tooltip("Time to reach target value (approx). Lower is faster.")]
        [SerializeField, Range(0, 0.5f)] private float smoothTime = 0.15f;
        [SerializeField] private bool fillAnimationOnStart = true;

        [Header("Visual Settings (Icon)")]
        [Tooltip("How long the pulse effect lasts in seconds.")]
        [SerializeField] private float iconPulseDuration = 0.2f;

        [Tooltip("Scale multiplier at the peak of the pulse.")]
        [SerializeField] private float iconPulseScale = 1.3f;

        [Tooltip("Color to flash when resource changes.")]
        [SerializeField] private Color iconFlashColor = Color.white;

        [Header("Arc Settings")]
        [SerializeField, Range(0, 360)] private int endDegreeValue = 360;

        [Header("Gradient Settings")]
        [SerializeField] private bool useGradient;
        [SerializeField] private Gradient barGradient;

        [Header("Events")]
        [SerializeField] private UnityEvent barIsFilledUp;

        // --- State Tracking (Bar) ---
        private float _visualFillAmount; // Where the bar currently IS
        private float _targetFillAmount; // Where the bar WANTS to be
        private float _currentVelocity; // For SmoothDamp

        // --- State Tracking (Icon) ---
        private Vector3 _defaultIconScale;
        private Color _defaultIconColor;
        private float _iconAnimationTimer;
        private bool _isIconAnimating;

        private bool _isInitialized;
        private bool _animate; // Main gate for Update loop

        // --- Enums ---
        public enum ShapeType
        {
            [InspectorName("Rectangle (Horizontal)")] RectangleHorizontal,
            [InspectorName("Rectangle (Vertical)")] RectangleVertical,
            Circle,
            Arc
        }

        public enum DisplayType
        {
            [InspectorName("Long (50|100)")] LongValue,
            [InspectorName("Short (50)")] ShortValue,
            [InspectorName("Percent (85%)")] Percentage,
            None
        }

        #region Unity Lifecycle

        private void OnValidate()
        {
            // Editor-time preview
            if (bar != null) ConfigureBarShape();
            SnapToTarget();
        }

        private void Awake()
        {
            if (bar == null)
            {
                Debug.LogError($"[ResourceBarTracker] Missing Image reference on {name}", this);
                enabled = false;
                return;
            }

            ConfigureBarShape();
            CacheIconDefaults();

            // Initial Setup
            _targetFillAmount = CalculateTargetFill();

            if (fillAnimationOnStart)
            {
                _visualFillAmount = 0f; // Start empty and animate up
                _animate = true;
            }
            else
            {
                SnapToTarget();
            }

            _isInitialized = true;
        }

        private void Update()
        {
            // Optimization: Global gate to stop processing completely if nothing is happening
            if (!_animate || !_isInitialized) return;

            // Process independent animations
            bool barBusy = ProcessBarAnimation();
            bool iconBusy = ProcessIconAnimation();

            // Only sleep the Update loop if BOTH systems are settled
            if (!barBusy && !iconBusy)
            {
                _animate = false;
            }
        }

        #endregion

        #region Animation Logic

        /// <summary>
        /// Handles the smooth filling of the bar. Returns true if animating.
        /// </summary>
        private bool ProcessBarAnimation()
        {
            // If bar is disabled or hidden, skip calculation but treat as "done"
            if (!bar.enabled || Mathf.Abs(_visualFillAmount - _targetFillAmount) < 0.001f)
            {
                if (Mathf.Abs(_visualFillAmount - _targetFillAmount) >= 0.001f)
                {
                    // Snap only if we are finishing the animation
                    SnapToTarget();
                    CheckFilledEvent();
                }
                return false;
            }

            // 1. Move Visual towards Target
            _visualFillAmount = Mathf.SmoothDamp(
                _visualFillAmount,
                _targetFillAmount,
                ref _currentVelocity,
                smoothTime
            );

            // 2. Apply to UI
            bar.fillAmount = _visualFillAmount;
            ApplyGradient(_visualFillAmount);

            return true;
        }

        /// <summary>
        /// Handles the pulse and flash of the icon using high-perf math. Returns true if animating.
        /// </summary>
        private bool ProcessIconAnimation()
        {
            if (!_isIconAnimating || resourceIcon == null) return false;

            _iconAnimationTimer += Time.deltaTime;
            float t = _iconAnimationTimer / iconPulseDuration;

            if (t >= 1.0f)
            {
                // Animation Complete: Reset to exact defaults
                resourceIcon.transform.localScale = _defaultIconScale;
                resourceIcon.color = _defaultIconColor;
                _isIconAnimating = false;
                return false;
            }

            // --- Math Logic ---
            // 1. Scale Pulse: Use Sin(t * PI) to go 0 -> 1 -> 0 smoothly
            // Multiplier adds the "punch" strength to the default scale
            float pulseFactor = Mathf.Sin(t * Mathf.PI);
            resourceIcon.transform.localScale = Vector3.LerpUnclamped(_defaultIconScale, _defaultIconScale * iconPulseScale, pulseFactor);

            // 2. Color Flash: Linear interpolation from FlashColor back to Default
            // This creates a flash on impact that fades out
            resourceIcon.color = Color.Lerp(iconFlashColor, _defaultIconColor, t);

            return true;
        }

        private void TriggerIconEffect()
        {
            // Condition: Effect must only trigger if Icon is active/enabled
            if (resourceIcon != null && resourceIcon.isActiveAndEnabled)
            {
                _iconAnimationTimer = 0f; // Reset timer to 0 to restart animation (allows spamming)
                _isIconAnimating = true;
                _animate = true; // Wake up Update loop
            }
        }

        #endregion

        #region Public API

        public bool ChangeResourceByAmount(int amount, bool animate = true)
        {
            if (!resourceData.OverkillPossible && resourceData.Current + amount < 0) return false;

            // Update Data
            resourceData.Current = Mathf.Clamp(resourceData.Current + amount, 0, resourceData.Max);

            // Calculate new visual target
            _targetFillAmount = CalculateTargetFill();
            UpdateText();

            // Trigger Visuals
            if (Application.isPlaying)
            {
                TriggerIconEffect(); // Icon reacts to change instantly

                if (animate)
                {
                    _animate = true; // Wake up Update loop for bar
                }
                else
                {
                    SnapToTarget();
                }
            }

            return true;
        }

        public void ChangeMaxAmountTo(int newMax, bool animate = true)
        {
            resourceData.Max = Mathf.Clamp(newMax, 0, resourceData.AbsoluteMax);
            resourceData.Current = Mathf.Clamp(resourceData.Current, 0, resourceData.Max);

            _targetFillAmount = CalculateTargetFill();
            UpdateText();

            if (Application.isPlaying)
            {
                TriggerIconEffect();

                if (animate)
                {
                    _animate = true;
                }
                else
                {
                    SnapToTarget();
                }
            }
        }

        public void SetBarVisibility(bool isVisible)
        {
            if (bar != null && bar.enabled != isVisible)
            {
                if (!isVisible)
                {
                    // Force complete bar animation if hiding
                    if (Mathf.Abs(_visualFillAmount - _targetFillAmount) > 0.01f)
                    {
                        SnapToTarget();
                    }
                    // We do not forcibly stop _animate here anymore, as the Icon might still be pulsing.
                    // The Update loop will naturally sleep when both tasks complete.
                }
                bar.enabled = isVisible;
            }
        }

        // --- Setup / Reset Wrappers ---

        public void Setup(int current, int max, int absMax, bool overkill, ShapeType shape, float speed,
            DisplayType display, bool useGrad, Gradient grad)
        {
            resourceData = new ResourceSettings
                { Current = current, Max = max, AbsoluteMax = absMax, OverkillPossible = overkill };
            shapeOfBar = shape;
            smoothTime = speed;
            valueDisplayMode = display;
            useGradient = useGrad;
            barGradient = grad;

            ConfigureBarShape();
            ChangeResourceByAmount(0, true);
        }

        public void ResetWithoutAnimation(int current, int max, int absMax)
        {
            resourceData.Current = current;
            resourceData.Max = max;
            resourceData.AbsoluteMax = absMax;

            _targetFillAmount = CalculateTargetFill();
            SnapToTarget();
        }

        #endregion

        #region Internal Logic

        private void CacheIconDefaults()
        {
            if (resourceIcon != null)
            {
                _defaultIconScale = resourceIcon.transform.localScale;
                _defaultIconColor = resourceIcon.color;
            }
        }

        private void ConfigureBarShape()
        {
            if (bar == null) return;

            switch (shapeOfBar)
            {
                case ShapeType.RectangleHorizontal: bar.fillMethod = Image.FillMethod.Horizontal; break;
                case ShapeType.RectangleVertical: bar.fillMethod = Image.FillMethod.Vertical; break;
                case ShapeType.Circle:
                case ShapeType.Arc: bar.fillMethod = Image.FillMethod.Radial360; break;
            }
        }

        private float CalculateTargetFill()
        {
            if (resourceData.Max <= 0) return 0f;

            float ratio = (float)resourceData.Current / resourceData.Max;

            if (shapeOfBar == ShapeType.Arc)
            {
                // Normalize ratio to the arc size
                return ratio * (endDegreeValue / 360f);
            }

            return ratio;
        }

        private void SnapToTarget()
        {
            if (bar == null) return;

            _targetFillAmount = CalculateTargetFill();

            if (bar.enabled)
            {
                _visualFillAmount = _targetFillAmount;
                bar.fillAmount = _visualFillAmount;
                ApplyGradient(_visualFillAmount);
                UpdateText();
            }
        }

        private void ApplyGradient(float currentFill)
        {
            if (!useGradient || barGradient == null)
            {
                if (bar.color != Color.white) bar.color = Color.white;
                return;
            }

            float evalTime = currentFill;
            if (shapeOfBar == ShapeType.Arc && endDegreeValue > 0)
            {
                evalTime = currentFill / (endDegreeValue / 360f);
            }

            bar.color = barGradient.Evaluate(evalTime);
        }

        private void UpdateText()
        {
            if (resourceValueTextField == null) return;

            // Note: SetText avoids some GC, but formatting numbers still creates minimal garbage.
            // For high-frequency "Ink" updates, consider a cached StringBuilder or
            // no-alloc integer formatter if Profiler shows this as a hotspot.
            switch (valueDisplayMode)
            {
                case DisplayType.LongValue:
                    resourceValueTextField.SetText("{0}/{1}", resourceData.Current, resourceData.Max);
                    break;
                case DisplayType.ShortValue:
                    resourceValueTextField.SetText("{0}", resourceData.Current);
                    break;
                case DisplayType.Percentage:
                    float percent = resourceData.Max > 0 ? ((float)resourceData.Current / resourceData.Max) * 100f : 0f;
                    resourceValueTextField.SetText("{0:0} %", percent);
                    break;
                case DisplayType.None:
                    resourceValueTextField.SetText(string.Empty);
                    break;
            }
        }

        private void CheckFilledEvent()
        {
            if (_visualFillAmount >= 0.999f)
            {
                barIsFilledUp?.Invoke();
            }
        }

        #endregion

        #region Tests

        [ContextMenu("Test -10")]
        public void TestSub() => ChangeResourceByAmount(-10);

        [ContextMenu("Test +10")]
        public void TestAdd() => ChangeResourceByAmount(10);

        #endregion
    }
}*/


/*
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Utilities.UI
{
    public class ResourceBarTracker : MonoBehaviour
    {
        #region Configuration Data

        [System.Serializable]
        public struct ResourceSettings
        {
            public int Current;
            public int Max;
            public int AbsoluteMax;
            public bool OverkillPossible;
        }

        #endregion

        [Header("References")] [SerializeField]
        private Image bar;

        [SerializeField] private TMP_Text resourceValueTextField;

        [Header("Resource Data")] [SerializeField]
        private ResourceSettings resourceData = new ResourceSettings { Current = 100, Max = 100, AbsoluteMax = 1000 };

        [Header("Visual Settings")] [SerializeField]
        private ShapeType shapeOfBar;

        [SerializeField] private DisplayType valueDisplayMode = DisplayType.Percentage;

        [Tooltip("Time to reach target value (approx). Lower is faster.")] [SerializeField, Range(0, 0.5f)]
        private float smoothTime = 0.15f;

        [SerializeField] private bool fillAnimationOnStart = true;

        [Header("Arc Settings")] [SerializeField, Range(0, 360)]
        private int endDegreeValue = 360;

        [Header("Gradient Settings")] [SerializeField]
        private bool useGradient;

        [SerializeField] private Gradient barGradient;
        private bool _animate;
        [Header("Events")] [SerializeField] private UnityEvent barIsFilledUp;

        // --- State Tracking ---
        private float _visualFillAmount; // Where the bar currently IS
        private float _targetFillAmount; // Where the bar WANTS to be
        private float _currentVelocity; // For SmoothDamp
        private bool _isInitialized;

        // --- Enums ---
        public enum ShapeType
        {
            [InspectorName("Rectangle (Horizontal)")]
            RectangleHorizontal,

            [InspectorName("Rectangle (Vertical)")]
            RectangleVertical,
            Circle,
            Arc
        }

        public enum DisplayType
        {
            [InspectorName("Long (50|100)")] LongValue,
            [InspectorName("Short (50)")] ShortValue,
            [InspectorName("Percent (85%)")] Percentage,
            None
        }

        #region Unity Lifecycle

        private void OnValidate()
        {
            // Editor-time preview
            if (bar != null) ConfigureBarShape();
            SnapToTarget();
        }

        private void Awake()
        {
            if (bar == null)
            {
                Debug.LogError($"[ResourceBarTracker] Missing Image reference on {name}", this);
                _animate = false;
                return;
            }

            ConfigureBarShape();

            // Initial Setup
            _targetFillAmount = CalculateTargetFill();

            if (fillAnimationOnStart)
            {
                _visualFillAmount = 0f; // Start empty and animate up
                _animate = true; // Start the Update loop
            }
            else
            {
                SnapToTarget();
            }

            _isInitialized = true;
        }

        // The "Consumer" Loop - Only runs when animating
        private void Update()
        {
            if (!_animate || !_isInitialized||!bar.enabled) return;
            // 1. Move Visual towards Target
            _visualFillAmount = Mathf.SmoothDamp(
                _visualFillAmount,
                _targetFillAmount,
                ref _currentVelocity,
                smoothTime
            );

            // 2. Apply to UI
            bar.fillAmount = _visualFillAmount;
            ApplyGradient(_visualFillAmount);

            // 3. Optimization: Sleep if close enough
            if (Mathf.Abs(_visualFillAmount - _targetFillAmount) < 0.001f)
            {
                SnapToTarget(); // Ensure final exact value
                CheckFilledEvent();
                _animate = false; // Stop Update loop to save CPU
            }
        }

        #endregion

        #region Public API

        public bool ChangeResourceByAmount(int amount, bool animate = true)
        {
            if (!resourceData.OverkillPossible && resourceData.Current + amount < 0) return false;

            // Update Data
            resourceData.Current = Mathf.Clamp(resourceData.Current + amount, 0, resourceData.Max);

            // Calculate new visual target
            _targetFillAmount = CalculateTargetFill();
            UpdateText(); // Text updates instantly (gameplay accurate), bar lags slightly (visual feel)

            if (animate && Application.isPlaying/*&&bar.enabled#1#)
            {
                _animate = true; // Wake up the Update loop
            }
            else
            {
                SnapToTarget();
            }

            return true;
        }

        public void ChangeMaxAmountTo(int newMax, bool animate = true)
        {
            resourceData.Max = Mathf.Clamp(newMax, 0, resourceData.AbsoluteMax);
            resourceData.Current = Mathf.Clamp(resourceData.Current, 0, resourceData.Max);

            _targetFillAmount = CalculateTargetFill();
            UpdateText();

            if (animate && Application.isPlaying)
            {
                _animate = true;
            }
            else
            {
                SnapToTarget();
            }
        }

        public void SetBarVisibility(bool isVisible)
        {
            if (bar != null&& bar.enabled != isVisible)
            {
                // If hiding, we can stop calculating animations
                if (!isVisible)
                {
                    _animate = false;
                    if (Mathf.Abs(_visualFillAmount - _targetFillAmount) > 0.01f)
                    {
                     SnapToTarget();
                    }

                }

                bar.enabled = isVisible;

            }
        }

        // --- Setup / Reset Wrappers ---

        public void Setup(int current, int max, int absMax, bool overkill, ShapeType shape, float speed,
            DisplayType display, bool useGrad, Gradient grad)
        {
            resourceData = new ResourceSettings
                { Current = current, Max = max, AbsoluteMax = absMax, OverkillPossible = overkill };
            shapeOfBar = shape;
            smoothTime = speed; // Note: Renamed animationTime to smoothTime for clarity
            valueDisplayMode = display;
            useGradient = useGrad;
            barGradient = grad;

            ConfigureBarShape();
            ChangeResourceByAmount(0, true); // Trigger visual update
        }

        public void ResetWithoutAnimation(int current, int max, int absMax)
        {
            resourceData.Current = current;
            resourceData.Max = max;
            resourceData.AbsoluteMax = absMax;

            _targetFillAmount = CalculateTargetFill();
            SnapToTarget();
        }

        #endregion

        #region Internal Logic

        private void ConfigureBarShape()
        {
            if (bar == null) return;

            switch (shapeOfBar)
            {
                case ShapeType.RectangleHorizontal: bar.fillMethod = Image.FillMethod.Horizontal; break;
                case ShapeType.RectangleVertical: bar.fillMethod = Image.FillMethod.Vertical; break;
                case ShapeType.Circle:
                case ShapeType.Arc: bar.fillMethod = Image.FillMethod.Radial360; break;
            }
        }

        private float CalculateTargetFill()
        {
            if (resourceData.Max <= 0) return 0f;

            float ratio = (float)resourceData.Current / resourceData.Max;

            if (shapeOfBar == ShapeType.Arc)
            {
                // Normalize ratio to the arc size
                return ratio * (endDegreeValue / 360f);
            }

            return ratio;
        }

        private void SnapToTarget()
        {
            if (bar == null) return;

            _targetFillAmount = CalculateTargetFill(); // Ensure current data is used

            if (bar.enabled)
            {
                _visualFillAmount = _targetFillAmount;
                bar.fillAmount = _visualFillAmount;
                ApplyGradient(_visualFillAmount);
                UpdateText();

            }



            //if (Application.isPlaying) enabled = false;
        }

        private void ApplyGradient(float currentFill)
        {
            if (!useGradient || barGradient == null)
            {
                if (bar.color != Color.white) bar.color = Color.white;
                return;
            }

            // Normalization: If we are an Arc, 100% health isn't fillAmount 1.0, it's fillAmount 0.X
            // We need to map that back to 0-1 for the Gradient evaluator.
            float evalTime = currentFill;
            if (shapeOfBar == ShapeType.Arc && endDegreeValue > 0)
            {
                evalTime = currentFill / (endDegreeValue / 360f);
            }

            bar.color = barGradient.Evaluate(evalTime);
        }

        private void UpdateText()
        {
            if (resourceValueTextField == null) return;

            // Garbage-Free updates
            switch (valueDisplayMode)
            {
                case DisplayType.LongValue:
                    resourceValueTextField.SetText("{0}/{1}", resourceData.Current, resourceData.Max);
                    break;
                case DisplayType.ShortValue:
                    resourceValueTextField.SetText("{0}", resourceData.Current);
                    break;
                case DisplayType.Percentage:
                    float percent = resourceData.Max > 0 ? ((float)resourceData.Current / resourceData.Max) * 100f : 0f;
                    resourceValueTextField.SetText("{0:0} %", percent);
                    break;
                case DisplayType.None:
                    resourceValueTextField.SetText(string.Empty);
                    break;
            }
        }

        private void CheckFilledEvent()
        {
            // Invoke event if we are essentially full
            if (_visualFillAmount >= 0.999f) // Tolerance for float errors
            {
                // To prevent spamming, you might want to track if we already invoked it
                // For now, this matches original behavior logic
                barIsFilledUp?.Invoke();
            }
        }

        #endregion

        #region Tests

        [ContextMenu("Test -10")]
        public void TestSub() => ChangeResourceByAmount(-10);

        [ContextMenu("Test +10")]
        public void TestAdd() => ChangeResourceByAmount(10);

        #endregion
    }
}*/
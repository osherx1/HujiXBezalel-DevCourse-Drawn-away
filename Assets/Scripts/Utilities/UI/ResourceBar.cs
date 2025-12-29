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

            if (animate && Application.isPlaying/*&&bar.enabled*/)
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
}
/*using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Utilities.UI
{
    public class ResourceBarTracker : MonoBehaviour
    {
        [Header("Core Settings")] [SerializeField]
        private Image bar;

        [SerializeField] private int resourceCurrent = 100;
        [SerializeField] private int resourceMax = 100;
        [SerializeField] private int resourceAbsoluteMax = 1000;
        [Space] [SerializeField] private bool overkillPossible;
        [Space] [SerializeField] private ShapeType shapeOfBar;

        public enum ShapeType
        {
            [InspectorName("Rectangle (Horizontal)")]
            RectangleHorizontal,

            [InspectorName("Rectangle (Vertical)")]
            RectangleVertical,
            [InspectorName("Circle")] Circle,
            Arc
        }
        [SerializeField] private bool fillAnimationOnStart = true;

        [Header("Arc Settings")] [SerializeField, Range(0, 360)]
        private int endDegreeValue = 360;

        [Header("Animation Speed")] [SerializeField, Range(0, 0.5f)]
        private float animationTime = 0.25f;

        private Coroutine _fillRoutine;


        [Header("Text Settings")] [SerializeField]
        private DisplayType howToDisplayValueText = DisplayType.Percentage;

        [SerializeField] private TMP_Text resourceValueTextField;

        public enum DisplayType
        {
            [InspectorName("Long (50|100)")] LongValue,
            [InspectorName("Short (50)")] ShortValue,
            [InspectorName("Percent (85%)")] Percentage,
            None
        }

        [Header("Gradient Settings")] [SerializeField]
        private bool useGradient;

        [SerializeField] private Gradient barGradient;

        [Header("Events")] [SerializeField] private UnityEvent barIsFilledUp;
        private float _previousFillAmount;

        [Header("Test mode")] [SerializeField] private bool enableTesting;


        private void OnValidate()
        {
            ConfigureBarShapeAndProperties();
        }

        private void Start()
        {
            if (fillAnimationOnStart)
            {
                TriggerFillAnimation();
            }
            else
            {
                TriggerFill();
            }

        }

        private void ConfigureBarShapeAndProperties()
        {
            switch (shapeOfBar)
            {
                case ShapeType.RectangleHorizontal:
                    bar.fillMethod = Image.FillMethod.Horizontal;
                    break;
                case ShapeType.RectangleVertical:
                    bar.fillMethod = Image.FillMethod.Vertical;
                    break;
                case ShapeType.Circle:
                case ShapeType.Arc:
                    bar.fillMethod = Image.FillMethod.Radial360;
                    //bar.fillOrigin = (int)Image.Origin360.Top;
                    break;
            }

            if (!useGradient)
                bar.color = Color.white;

            UpdateBarAndResourceText();
        }

        private void UpdateBarAndResourceText()
        {
            if (resourceMax <= 0)
            {
                bar.fillAmount = 0;
                SetCurrentResourceValueText();
                return;
            }

            float fillAmount = CalculateTargetFill();
            bar.fillAmount = fillAmount;
            SetCurrentResourceValueText();
        }

        private float CalculateCircularFillAmount()
        {
            float fraction = (float)resourceCurrent / resourceMax;
            float fillRange = endDegreeValue / 360f;

            return fillRange * fraction;
        }

        private void SetCurrentResourceValueText()
        {
            switch (howToDisplayValueText)
            {
                case DisplayType.LongValue:
                    resourceValueTextField.SetText($"{resourceCurrent}/{resourceMax}");
                    break;
                case DisplayType.ShortValue:
                    resourceValueTextField.SetText($"{resourceCurrent}");
                    break;
                case DisplayType.Percentage:
                    float percentage = ((float)resourceCurrent / resourceMax) * 100;
                    resourceValueTextField.SetText($"{Mathf.RoundToInt(percentage)} %");
                    break;
                case DisplayType.None:
                    resourceValueTextField.SetText(string.Empty);
                    break;
            }
        }



        public bool ChangeResourceByAmount(int amount,bool animate=true)
        {
            if (!overkillPossible && resourceCurrent + amount < 0)
                return false;

            resourceCurrent += amount;
            resourceCurrent = Mathf.Clamp(resourceCurrent, 0, resourceMax);
            if (animate)
            {
                TriggerFillAnimation();
            }
            else
            {
                TriggerFill();
            }
            
            return true;
        }

        private void TriggerFill()
        {
            float targetFill = CalculateTargetFill();

            if (Mathf.Approximately(bar.fillAmount, targetFill))
                return;

            ResetFillRoutine();
            bar.fillAmount = resourceCurrent;
            UseGradient();
            HandleEvent();
            _previousFillAmount = bar.fillAmount;
            SetCurrentResourceValueText();
        }

        private void ResetFillRoutine()
        {
            if (_fillRoutine != null)
            {
                StopCoroutine(_fillRoutine);
                _fillRoutine = null;
            }
        }

        private void TriggerFillAnimation()
        {
            float targetFill = CalculateTargetFill();

            if (Mathf.Approximately(bar.fillAmount, targetFill))
                return;

            ResetFillRoutine();

            _fillRoutine = StartCoroutine(SmoothlyTransitionToNewValue(targetFill));
            SetCurrentResourceValueText();
        }

        private float CalculateTargetFill()
        {
            if (shapeOfBar == ShapeType.Arc)
                return CalculateCircularFillAmount();

            return (float)resourceCurrent / resourceMax;
        }

        private IEnumerator SmoothlyTransitionToNewValue(float targetFill)
        {
            float originalFill = bar.fillAmount;
            float elapsedTime = 0.0f;

            while (elapsedTime < animationTime)
            {
                elapsedTime += Time.deltaTime;
                float time = elapsedTime / animationTime;
                bar.fillAmount = Mathf.Lerp(originalFill, targetFill, time);

                UseGradient();

                yield return null;
            }

            bar.fillAmount = targetFill;

            HandleEvent();
            _previousFillAmount = bar.fillAmount;
        }

        private void UseGradient()
        {
            if (!useGradient)
                return;

            if (shapeOfBar == ShapeType.Arc)
            {
                float fillRange = bar.fillAmount / (endDegreeValue / 360f);
                bar.color = barGradient.Evaluate(fillRange);
                return;
            }

            bar.color = barGradient.Evaluate(bar.fillAmount);
        }


        private void HandleEvent()
        {
            if (_previousFillAmount >= 1)
                return;

            if (bar.fillAmount >= 1)
                barIsFilledUp?.Invoke();
        }

        public void ChangeMaxAmountTo(int newMaxAmount,bool animate=true)
        {
            newMaxAmount = Mathf.Clamp(newMaxAmount, 0, resourceAbsoluteMax);

            resourceMax = newMaxAmount;
            resourceCurrent = Mathf.Clamp(resourceCurrent, 0, resourceMax);
            if (animate)
            {
                TriggerFillAnimation();
            }
            else
            {
                TriggerFill();
            }

            
        }

        public void Setup(int resourceCurrent, int resourceMax, int resourceAbsoluteMax, bool overkillPossible,
            ShapeType shapeOfBar,
            float animationTime, DisplayType howToDisplayValueText, bool useGradient, Gradient barGradient)
        {
            this.resourceCurrent = resourceCurrent;
            this.resourceMax = resourceMax;
            this.resourceAbsoluteMax = resourceAbsoluteMax;
            this.overkillPossible = overkillPossible;
            this.shapeOfBar = shapeOfBar;

            this.animationTime = animationTime;

            this.howToDisplayValueText = howToDisplayValueText;

            this.useGradient = useGradient;
            this.barGradient = barGradient;


            ConfigureBarShapeAndProperties();
            TriggerFillAnimation();
        }

        public void ResetWithoutAnimation(int resourceCurrent, int resourceMax, int resourceAbsoluteMax,
            bool overkillPossible, ShapeType shapeOfBar,
            DisplayType howToDisplayValueText, bool useGradient, Gradient barGradient)
        {
            ResetFillRoutine();
            this.resourceCurrent = resourceCurrent;
            this.resourceMax = resourceMax;
            this.resourceAbsoluteMax = resourceAbsoluteMax;
            this.overkillPossible = overkillPossible;
            this.shapeOfBar = shapeOfBar;
            this.howToDisplayValueText = howToDisplayValueText;
            this.useGradient = useGradient;
            this.barGradient = barGradient;

            ConfigureBarShapeAndProperties();

 

            UpdateBarAndResourceText();
        }

        public void ResetWithoutAnimation(int resourceCurrent, int resourceMax, int resourceAbsoluteMax)
        {
            ResetWithoutAnimation(resourceCurrent, resourceMax, resourceAbsoluteMax, this.overkillPossible,
                this.shapeOfBar,
                this.howToDisplayValueText, this.useGradient, this.barGradient);
        }

        private void OnDisable()
        {
            ResetFillRoutine();
            UpdateBarAndResourceText();
            if (useGradient)

            {
                UseGradient();
            }
        }
        
        public void ChangeResourceByAmountTest(int amount)
        {
            ChangeResourceByAmount(amount);
        }
        
        // --- NEW: Toggle Visibility Function ---
        /// <summary>
        /// Sets the visibility of the bar image.
        /// Does not affect the text or logic, only the main bar sprite.
        /// </summary>
        /// <param name="isVisible">True to show, False to hide.</param>
        public void SetBarVisibility(bool isVisible)
        {
            if (bar != null)
            {
                ResetFillRoutine();
                bar.enabled = isVisible;
            }
        }
    }
}*/
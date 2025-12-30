using System;
using System.Collections;
using Drawing.Data;
using Drawing.LineControl;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.UI;
using Utilities.UI;

namespace Drawing
{
    public class DrawingConfigButton : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Button targetButton;

        [Header("Values to Apply")] [SerializeField]
        private LineSettingsCollection lineSettingsCollection;

        [SerializeField] private string settingID = "Default";

        [Header("Visual Feedback State")] [SerializeField]
        private Color selectedColor = Color.green;
        [SerializeField] private Image markingToolImage;
        
        [Header("Fill Capacity Settings")]
        [SerializeField] private int maxFillAmount = 1000;
        [SerializeField] private int finishLineCost = 1;
        [SerializeField] private ResourceBarTracker resourceBarTracker;

        [Header("Preview UI References")]
        [SerializeField] private ResourceBarTracker previewSelectedToolBar;
        [SerializeField] private GameObject previewSelectedToolIcon;

        //[SerializeField] private UIVisualFeedback feedbackEffects;

        private LineSettings _valuesToApply;
        private Color _normalColor;
        private Image _targetButtonImage;
        private bool _isSelected = false;
        private int _currentFillAmount;
        
        public event Action<int,int, int> OnInkChanged;
        public int CurrentInk => _currentFillAmount;
        public int FinishLineCost => finishLineCost;
        public int MaxInk => maxFillAmount;
        



        private void Awake()
        {
            if (targetButton == null)
            {
                Debug.LogError($"Target Button is missing in {name}");
                enabled = false;
                return;
            }

            _targetButtonImage = targetButton.GetComponent<Image>();

            if (_targetButtonImage != null)
            {
                _normalColor = _targetButtonImage.color;
            }

            _currentFillAmount = maxFillAmount;

            if (resourceBarTracker != null)
            {
                resourceBarTracker.ChangeMaxAmountTo(maxFillAmount);
                resourceBarTracker.ChangeResourceByAmount(_currentFillAmount);
            }

      
            if(previewSelectedToolIcon != null)
            {
                previewSelectedToolIcon.SetActive(false);
            }

            InitializeFromCollection();
        }

        private void Start()
        {
            if (previewSelectedToolBar != null)
            {
                previewSelectedToolBar.ChangeMaxAmountTo(maxFillAmount, false);
                previewSelectedToolBar.ChangeResourceByAmount(_currentFillAmount, false);
                //selectToolBar.ResetWithoutAnimation(_currentFillAmount, maxFillAmount, 1000);
                previewSelectedToolBar.SetBarVisibility(false);
            }

            if (markingToolImage != null)
            {
                markingToolImage.enabled = false;
            }
        }

        private void InitializeFromCollection()
        {
            LineSettings settings = lineSettingsCollection.GetSettingsByID(settingID);
            if (settings != null)
            {
                _valuesToApply = settings;
            }
            else
            {
                Debug.LogWarningFormat("Setting '{0}' does not exist.", settingID);
            }
        }

        private void OnEnable()
        {
            if (targetButton != null)
                targetButton.onClick.AddListener(OnButtonClicked);

            EventManager.Instance.OnConfigButtonSelected += OnGlobalConfigChanged;
            Line.onLineDestroyed += HandleRefundInk;

            /*if (resourceBarTracker != null && resourceBarTracker.gameObject.activeInHierarchy) 
            {
                resourceBarTracker.SetBarVisibility(true);
            }*/
            
        }

        private void OnDisable()
        {
            if (targetButton != null)
                targetButton.onClick.RemoveListener(OnButtonClicked);

            EventManager.Instance.OnConfigButtonSelected -= OnGlobalConfigChanged;
            Line.onLineDestroyed -= HandleRefundInk;

            /*if (resourceBarTracker != null)
            {
                resourceBarTracker.SetBarVisibility(false);
            }
            if (selectToolBar != null)
            {
                selectToolBar.SetBarVisibility(false);
            }*/
            /*if(previewSelectToolIcon!= null)
            {
                previewSelectToolIcon.SetActive(false);
            }*/
        }

        private void HandleRefundInk(string lineName, int amount)
        {
            if (lineName != settingID) return;
            RefundInk(amount);
        }

        public bool TryConsumeInk(float amount)
        {
            
            //TODO - Maybe use > on the amount of ink
            if (_currentFillAmount >= amount)
            {
                if (amount != 0)
                {
                    _currentFillAmount -= (int)amount;
                    NotifyInkChanged(-(int) amount, true, _isSelected);
                }

                return true;
            }

            return false;
        }

        public void ResetInk()
        {
            var tempAmount = _currentFillAmount;
            _currentFillAmount = 0;
            NotifyInkChanged(-tempAmount, true, _isSelected);

        }
        /*if (_currentFillAmount >= amount)
        {
            _currentFillAmount -= (int)amount;

            if (resourceBarTracker != null)
            {
                resourceBarTracker.ChangeResourceByAmount(-(int)amount);
            }
            return true;
        }
        return false;*/


        public void RefundInk(float amount)
        {
            _currentFillAmount = Mathf.Clamp(_currentFillAmount + (int)amount, 0, maxFillAmount);

            NotifyInkChanged((int) amount,true, _isSelected);
 
        }

        private void OnButtonClicked()
        {
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);

            if (_isSelected) return;

            SetSelectionState(true);
            ApplySettings();
            EventManager.Instance.TriggerConfigButtonSelected(this);
        }

        private void OnGlobalConfigChanged(object sender)
        {
            if (sender != this)
            {
                SetSelectionState(false);
            }
        }

        private void SetSelectionState(bool isSelected)
        {
            _isSelected = isSelected;

            if (_targetButtonImage != null)
            {
                _targetButtonImage.color = isSelected ? selectedColor : _normalColor;
            }

            if (previewSelectedToolBar != null)
            {
                previewSelectedToolBar.SetBarVisibility(isSelected);
            }

            if (previewSelectedToolIcon != null)
            { 
                previewSelectedToolIcon.SetActive(isSelected);
            }
            if(markingToolImage != null)
            {
                markingToolImage.enabled = isSelected;
            }
            /*if(feedbackEffects!= null)
            {
                feedbackEffects.gameObject.SetActive(isSelected);
            }*/
            
        }

        private void ApplySettings()
        {
            if (DrawingConfigController.Instance == null) return;

            var controller = DrawingConfigController.Instance;
            controller.SetLineSetting(_valuesToApply);
            controller.SetButton(this);
            if (previewSelectedToolBar != null)
            {
                previewSelectedToolBar.SetBarVisibility(true);
                previewSelectedToolBar.ChangeResourceByAmount(0, false);
            }
            if(markingToolImage != null)
            {
                markingToolImage.enabled = true;
            }
        }

        public Image GetTargetButtonImage()
        {
            if (_targetButtonImage == null)
            {
                Debug.LogError($"Target Button is missing in {name}");
            }
            return _targetButtonImage;

        }
        private void NotifyInkChanged(int delta,bool animatePopBar, bool animateSelectBar )
        {
            //OnInkChanged?.Invoke(delta, _currentFillAmount, maxFillAmount);
            
            if (resourceBarTracker != null)
                resourceBarTracker.ChangeResourceByAmount(delta, animatePopBar);
                
            if (previewSelectedToolBar != null)
                previewSelectedToolBar.ChangeResourceByAmount(delta, animateSelectBar);
            //TriggerJuice(delta);
        }

        

        /*private void TriggerJuice(int delta)
        {
            // FIX: Ensure feedback object exists and is enabled before calling
            if (!_isSelected || feedbackEffects == null || !feedbackEffects.isActiveAndEnabled) return;

            if (delta < 0)
            {
                // SPENDING INK: Shake and Flash
                feedbackEffects.PlayShake();
                //feedbackEffects.PlayFlash();
            }
            else if (delta > 0)
            {
                // REGAINING INK: Pulse
                feedbackEffects.PlayPulse();
            }*/
        
        
        
        

    }
}

/*using Drawing.Data;
using Drawing.LineControl;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.UI;
using Utilities.UI;

namespace Drawing
{
    [RequireComponent(typeof(Button))]
    public class DrawingConfigButton : MonoBehaviour
    {
        [Header("Values to Apply")] [SerializeField]
        private LineSettingsCollection lineSettingsCollection;

        [SerializeField] private string settingID = "Default";
        private LineSettings valuesToApply;

        [Header("Visual Feedback State")] private Color normalColor;
        [SerializeField] private Color selectedColor = Color.green;
        private Button _button;
        private bool _isSelected = false;
        private Image _buttonImage;
        [SerializeField] private int maxFillAmount = 1000;
        private int _currentFillAmount;
        [SerializeField] private ResourceBarTracker resourceBarTracker;

        private void Awake()
        {
            // Cache references to avoid repetitive GetComponent calls
            _button = GetComponent<Button>();
            _buttonImage = GetComponent<Image>();
            normalColor = _buttonImage.color;
            _currentFillAmount = maxFillAmount;

            if(resourceBarTracker!= null)

            {

                resourceBarTracker.ChangeMaxAmountTo((int)
                maxFillAmount);

                resourceBarTracker.ChangeResourceByAmount((int)_currentFillAmount);

            }




            //_buttonImage.color = normalColor;
            InitializeFromCollection();
        }

        private void InitializeFromCollection()
        {
            LineSettings settings = lineSettingsCollection.GetSettingsByID(settingID);
            if (settings != null)
            {
                valuesToApply = settings;
            }
            else
            {
                Debug.LogWarningFormat("Setting '{0}' does not exist.", settingID);
            }
        }

        private void OnEnable()
        {
            // Listen for local click events
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);

            // Subscribe to the global event to know when OTHER buttons are clicked
            EventManager.Instance.OnConfigButtonSelected += OnGlobalConfigChanged;
            Line.onLineDestroyed += HandleRefundInk;
            if (resourceBarTracker != null)
            {
                resourceBarTracker.SetBarVisibility(true);
            }

        }

        private void HandleRefundInk(string lineName, int amount)
        {
            if (lineName != settingID) return;
            RefundInk(amount);
        }

        private void OnDisable()
        {
            // Always unsubscribe from events to prevent memory leaks
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);

            EventManager.Instance.OnConfigButtonSelected -= OnGlobalConfigChanged;
            Line.onLineDestroyed -= HandleRefundInk;
            if (resourceBarTracker != null)
            {
                resourceBarTracker.SetBarVisibility(false);
            }
        }


        /// <summary>
        /// Attempts to subtract ink. Returns true if successful.
        /// </summary>
        public bool TryConsumeInk(float amount)
        {
            if (_currentFillAmount >= amount)
            {
                //TODO - Maybe use upperbound on the amount of ink
                _currentFillAmount -= (int)amount;
                if (resourceBarTracker != null)
                {
                    resourceBarTracker.ChangeResourceByAmount(-(int)amount);
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// Restores ink to this button (e.g. when line is deleted).
        /// </summary>
        public void RefundInk(float amount)
        {
            //TODO - Maybe use upperbound on the amount of ink refunded
            _currentFillAmount = Mathf.Clamp(_currentFillAmount + (int)amount, 0, maxFillAmount);
            if (resourceBarTracker != null)
            {
                resourceBarTracker.ChangeResourceByAmount((int)amount);
            }

        }


        /// <summary>
        /// Called when the user clicks THIS button.
        /// </summary>
        private void OnButtonClicked()
        {
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);
            // Optimization: If already selected, do nothing (optional behavior)
            if (_isSelected) return;

            // 1. Update internal state to Active
            SetSelectionState(true);

            // 2. Push the configuration to the central controller
            ApplySettings();

            // 3. Notify the EventManager that this specific button was selected.
            // We pass 'this' as the sender so we don't reset ourselves in the event callback.
            EventManager.Instance.TriggerConfigButtonSelected(this);
        }

        /// <summary>
        /// Callback triggered whenever ANY configuration button is selected.
        /// </summary>
        /// <param name="sender">The specific button script that triggered the event.</param>
        private void OnGlobalConfigChanged(object sender)
        {
            // If the button that triggered the event is NOT this one,
            // it means another button was clicked. We must deactivate (reset) this one.
            if (sender != this)
            {
                SetSelectionState(false);
            }
        }

        /// <summary>
        /// Updates the logical state and visual appearance of the button.
        /// </summary>
        /// <param name="isSelected">True for Active/Selected, False for Inactive.</param>
        private void SetSelectionState(bool isSelected)
        {
            _isSelected = isSelected;

            // Update visual feedback (change color)
            if (_buttonImage != null)
            {
                _buttonImage.color = isSelected ? selectedColor : normalColor;
            }
        }

        private void ApplySettings()
        {
            if (DrawingConfigController.Instance == null) return;

            var controller = DrawingConfigController.Instance;
            controller.SetLineSetting(valuesToApply);
            controller.SetButton(this);
        }
    }
}*/
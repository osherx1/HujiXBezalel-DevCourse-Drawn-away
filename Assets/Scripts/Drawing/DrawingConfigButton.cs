using Drawing.Data;
using Drawing.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Drawing
{
    [RequireComponent(typeof(Button))]
    public class DrawingConfigButton : MonoBehaviour
    {
        [Header("Values to Apply")] [SerializeField]
        private LineSettingsCollection lineSettingsCollection;

        [SerializeField] private string settingID = "Default";
        private LineSettings valuesToApply;


        [Header("What to Override?")]
        [Space(10)]
        [Header("Visual Settings")]
        [SerializeField] private bool applyColor;
        [SerializeField] private bool applyMaterial;
        [SerializeField] private bool applyLineWidth;
        [SerializeField] private bool applyCapVertices;
        [SerializeField] private bool applyCornerVertices;
        [Space(2)]
        [Header("Physics Settings")]
        [SerializeField] private bool applyUsePhysics;
        [SerializeField] private bool applyPhysicsMaterial;
        [SerializeField] private bool applyGravity;
        [SerializeField] private bool applyMass;
        

        


        [Header("Visual Feedback State")] private Color normalColor;
        [SerializeField] private Color selectedColor = Color.green;
        private Button _button;
        private bool _isSelected = false;
        private Image _buttonImage;

        private void Awake()
        {
            // Cache references to avoid repetitive GetComponent calls
            _button = GetComponent<Button>();
            _buttonImage = GetComponent<Image>();
            normalColor = _buttonImage.color;
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
        }

        private void OnDisable()
        {
            // Always unsubscribe from events to prevent memory leaks
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);

            EventManager.Instance.OnConfigButtonSelected -= OnGlobalConfigChanged;
        }


        /// <summary>
        /// Called when the user clicks THIS button.
        /// </summary>
        private void OnButtonClicked()
        {
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

            // Injecting: (Do we want to override?, The value to use if we do)

            controller.SetColor(valuesToApply.lineColor, applyColor);

            controller.SetWidth(valuesToApply.lineWidth, applyLineWidth);

            controller.SetUsePhysics(valuesToApply.usePhysics, applyUsePhysics);

            controller.SetPhysicsMaterial(valuesToApply.physicsMaterial, applyPhysicsMaterial);

            controller.SetGravity(valuesToApply.gravityScaleOverride, applyGravity);
            controller.SetMassMult(valuesToApply.massMult, applyMass);
            controller.SetMaterial(valuesToApply.material, applyMaterial);
            controller.SetCapVertices(valuesToApply.endCapVertices, applyCapVertices);
            controller.SetCornerVertices(valuesToApply.cornerVertices, applyCornerVertices);
        }
    }
}
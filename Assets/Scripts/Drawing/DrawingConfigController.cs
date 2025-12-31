using System;
using Drawing.Data;
using Drawing.Managers;
using UnityEngine;

namespace Drawing
{
    public class DrawingConfigController : MonoBehaviour
    {
        public static DrawingConfigController Instance { get; private set; }

        [Header("Configuration")] [SerializeField]
        private LineSettingsCollection lineSettingsCollection;

        [SerializeField] private string _defaultSettingID = "Default";

        [Header("Debug")] [Tooltip("Toggle to enable/disable console logs for setting changes.")] [SerializeField]
        private bool showDebugLogs = true;

        private LineSettings defaultSettings;
        public LineSettings currentSettings = new LineSettings();
        private DrawingConfigButton _currentButton;
        [SerializeField] private DrawingConfigButton defaultButton;
        
        private bool _eraserActive = false;

        public LineSettings DefaultSettings => defaultSettings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Initialize Defaults
            LineSettings settings = lineSettingsCollection.GetSettingsByID(_defaultSettingID);
            if (settings != null)
            {
                defaultSettings = settings;
                Log($"Default settings loaded from ID '{_defaultSettingID}'.");
            }
            else
            {
                // Critical error - always log this, regardless of the bool
                Debug.LogWarning(
                    $"DrawingConfigController: Default settings with ID '{_defaultSettingID}' not found! Using hardcoded defaults.",
                    this);
                defaultSettings = new LineSettings(); // Prevent null reference
            }

            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            if (currentSettings == null) currentSettings = new LineSettings();
            Log("Resetting all settings to defaults.");
            currentSettings.SetLineSetting(defaultSettings);

        }

        public void SetButton(DrawingConfigButton drawingConfigButton)
        {
            _currentButton = drawingConfigButton;
        }


        public LineSettings GetCurrentSettings() => currentSettings;

        // --- HELPER METHODS ---

        /// <summary>
        /// Internal helper to log only if debug is enabled.
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[DrawingConfig]: {message}", this);
            }
        }

        /// <summary>
        /// Helper to format consistent state change messages.
        /// </summary>
        private void LogStateChange(string settingName, bool isOverride, object value)
        {
            if (!showDebugLogs) return;

            string status = isOverride ? "<color=cyan>Override</color>" : "<color=orange>Revert</color>";
            Log($"{settingName} -> {status}. New Value: <b>{value}</b>");
        }

        

        public void SetLineSetting(LineSettings valuesToApply)
        {
            currentSettings.SetLineSetting(valuesToApply);
        }

        public bool TryConsumeInk(float amount)
        {
            //Debug.Log("DrawingConfig TryConsumeInk called with amount: " + amount);
            if (_currentButton != null)
            {
                return _currentButton.TryConsumeInk(amount);
            }
            return false;
        }
        public int CheckInk()
        {
            if (_currentButton != null)
            {
                return _currentButton.CurrentInk;
            }
            return 0;
        }
        public void ResetInk()
        {
            if (_currentButton != null)
            {
                 _currentButton.ResetInk();
            }
        }

        private void OnEnable()
        {
            EventManager.Instance.OnEraserActive += EraserActive;
            EventManager.Instance.OnEraserInactive += EraserInactive;
        }

        private void OnDisable()
        {
            EventManager.Instance.OnEraserActive -= EraserActive;
            EventManager.Instance.OnEraserInactive -= EraserInactive;
        }
        public bool IsEraserActive()
        {
            return _eraserActive;
        }

        public void EraserActive()
        {
            _eraserActive = true;
        }
        public void EraserInactive()
        {
            _eraserActive = false;
        }

        public int FinishLineInkCost()
        {
            if (_currentButton != null)
            {
                return _currentButton.FinishLineCost;
            }

            return 1;
        }
    }
}
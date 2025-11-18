using Drawing.Data;
using UnityEngine;

namespace Drawing
{
    public class DrawingConfigController : MonoBehaviour
    {
        public static DrawingConfigController Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private LineSettingsCollection lineSettingsCollection;
        [SerializeField] private string _defaultSettingID = "Default";

        [Header("Debug")]
        [Tooltip("Toggle to enable/disable console logs for setting changes.")]
        [SerializeField] private bool showDebugLogs = true;

        private LineSettings defaultSettings;
        public LineSettings currentSettings = new LineSettings();

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
                Debug.LogWarning($"DrawingConfigController: Default settings with ID '{_defaultSettingID}' not found! Using hardcoded defaults.", this);
                defaultSettings = new LineSettings(); // Prevent null reference
            }

            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            Log("Resetting all settings to defaults.");
            
            // Copy all values from default to current
            SetColor(null, false);
            SetWidth(0, false);
            SetUsePhysics(false, false);
            SetPhysicsMaterial(null, false);
            SetGravity(0, false);
        }

        // --- SETTERS WITH LOGIC ---

        public void SetColor(Gradient newColor, bool overrideValue = false)
        {
            currentSettings.lineColor = overrideValue ? newColor : defaultSettings.lineColor; 
            //LogStateChange("Color", overrideValue, currentSettings.lineColor);
        }
        public void SetMassMult(float massMult, bool overrideValue = false)
        {
            currentSettings.massMult = overrideValue ? massMult : defaultSettings.massMult; 
            //LogStateChange("Color", overrideValue, currentSettings.lineColor);
        }
        public void SetMaterial(Material material, bool applyMaterial)
        {
            currentSettings.material = applyMaterial ? material : defaultSettings.material;
        }

        public void SetWidth(float newWidth, bool overrideValue = false)
        {
            currentSettings.lineWidth = overrideValue ? newWidth : defaultSettings.lineWidth;
            LogStateChange("Width", overrideValue, currentSettings.lineWidth);
        }

        public void SetUsePhysics(bool usePhysics, bool overrideValue = false)
        {
            currentSettings.usePhysics = overrideValue ? usePhysics : defaultSettings.usePhysics;
            LogStateChange("UsePhysics", overrideValue, currentSettings.usePhysics);
        }

        public void SetPhysicsMaterial(PhysicsMaterial2D newMat, bool overrideValue = false)
        {
            currentSettings.physicsMaterial = overrideValue ? newMat : defaultSettings.physicsMaterial;
            string matName = currentSettings.physicsMaterial != null ? currentSettings.physicsMaterial.name : "None";
            LogStateChange("PhysicsMaterial", overrideValue, matName);
        }

        public void SetGravity(float scale, bool overrideValue = false)
        {
            if (overrideValue)
            {
                currentSettings.gravityScaleOverride = scale;
            }
            else
            {
                currentSettings.gravityScaleOverride = defaultSettings.gravityScaleOverride;
            }
            LogStateChange("Gravity", overrideValue, currentSettings.gravityScaleOverride);
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
        
        

  

   
    }
}

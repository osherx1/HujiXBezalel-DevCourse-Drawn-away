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
            if(currentSettings == null) currentSettings = new LineSettings();
            Log("Resetting all settings to defaults.");
            currentSettings.lineWidth = defaultSettings.lineWidth;
            currentSettings.lineColor = defaultSettings.lineColor;
            currentSettings.material = defaultSettings.material;
            currentSettings.usePhysics = defaultSettings.usePhysics;
            currentSettings.physicsMaterial = defaultSettings.physicsMaterial;
            currentSettings.gravityScaleOverride = defaultSettings.gravityScaleOverride;
            currentSettings.massMult = defaultSettings.massMult;
            currentSettings.endCapVertices = defaultSettings.endCapVertices;
            currentSettings.cornerVertices = defaultSettings.cornerVertices;
            currentSettings.drawSound = defaultSettings.drawSound;
            currentSettings.collisionSound = defaultSettings.collisionSound;
            currentSettings.releaseSound = defaultSettings.releaseSound;
            
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


        public void SetCapVertices(int endCapVertices, bool overrideValue = false)
        {
            currentSettings.endCapVertices = overrideValue ? endCapVertices : defaultSettings.endCapVertices;

        }

        public void SetCornerVertices(int cornerVertices, bool overrideValue = false)
        {
            currentSettings.cornerVertices = overrideValue ? cornerVertices : defaultSettings.cornerVertices;
        }
        public void SetSoundSettings(GameSoundsSo.AudioType drawSound,
            GameSoundsSo.AudioType collisionSound,
            GameSoundsSo.AudioType releaseSound)
        {
            currentSettings.drawSound = drawSound;
            currentSettings.collisionSound = collisionSound;
            currentSettings.releaseSound = releaseSound;
            
        }
    }
}

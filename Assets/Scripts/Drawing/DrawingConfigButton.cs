using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Drawing
{
    /// <summary>
    /// Button component that applies drawing configuration settings when clicked.
    /// Attach this to a UI Button and configure the settings in the Inspector.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DrawingConfigButton : MonoBehaviour
    {
        /*
        [Header("Drawing Settings")]
        [Tooltip("Line width to apply when button is clicked. Leave at 0 to not change width.")]
        //[SerializeField] private float lineWidth = 0f;
        */

        /*
        [Tooltip("Whether to enable physics on drawn lines. Leave unchecked to not change physics setting.")]
        [SerializeField] private bool overrideUsePhysics = false;
        */

       // [SerializeField] private bool usePhysics = true;
       // [SerializeField] private bool changeGravityScale = false;
        

        /*
        [Tooltip("Physics material to apply. Leave null to not change material.")]
        [SerializeField] private PhysicsMaterial2D physicsMaterial = null;
        */

        //[Tooltip("Gravity scale override. Leave at 0 to not change gravity.")]
       // [SerializeField] private float gravityScaleOverride = 0f;

        [Header("Optional: Apply All Settings")]
        [Tooltip("If true, applies all configured settings. If false, only applies non-zero/null values.")]
        [SerializeField] private bool applyAllSettings = false;

        [SerializeField] private Button button;
        [SerializeField] private LineSettings lineSettings;

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
            else
            {
                Debug.LogWarning("DrawingConfigButton: Button is null. Cannot apply settings.", this);
            }
        }
        

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
            else
            {
                Debug.LogWarning("DrawingConfigButton: Button is null. Cannot apply settings.", this);
            }
        }

        /// <summary>
        /// Called when the button is clicked. Applies the configured settings to DrawingConfigController.
        /// </summary>
        private void OnButtonClicked()
        {
            if (DrawingConfigController.Instance == null)
            {
                Debug.LogWarning("DrawingConfigButton: DrawingConfigController.Instance is null. Cannot apply settings.", this);
                return;
            }

            if (applyAllSettings)
            {
                // Apply all settings regardless of values
                ApplyAllSettings();
            }
            else
            {
                // Apply only non-default/non-null values
                ApplySelectiveSettings();
            }
        }

        /// <summary>
        /// Applies all configured settings to DrawingConfigController.
        /// </summary>
        private void ApplyAllSettings()
        {
            //DrawingConfigController.Instance.SetWidth(lineWidth);
            DrawingConfigController.Instance.SetUsePhysics(lineSettings.usePhysics);

            if (lineSettings.physicsMaterial != null)
            {
                DrawingConfigController.Instance.SetPhysicsMaterial(lineSettings.physicsMaterial);
            }

            DrawingConfigController.Instance.SetGravity(lineSettings.gravityScaleOverride);
        }

        /// <summary>
        /// Applies only settings that have been explicitly configured (non-zero/null values).
        /// </summary>
        private void ApplySelectiveSettings()
        {
            DrawingConfigController.Instance.SetUsePhysics(lineSettings.usePhysics);
            if (lineSettings.changeColor)
            {
                DrawingConfigController.Instance.SetColor(lineSettings.lineColor);

                
            }
            

            // Apply width only if it's greater than 0
            /*if (lineWidth > 0f)
            {
                Debug.Log("Applying line width: " + lineWidth);
                DrawingConfigController.Instance.SetWidth(lineWidth);
            }*/

            // Apply physics setting only if override is enabled

            

            // Apply physics material only if one is assigned
            if (lineSettings.physicsMaterial != null)
            {
                Debug.Log("Applying physics material: " + lineSettings.physicsMaterial.name);
                DrawingConfigController.Instance.SetPhysicsMaterial(lineSettings.physicsMaterial);
            }
            

            if (lineSettings.changeGravityScale)
            {
                Debug.Log("Applying gravity scale: " + lineSettings.gravityScaleOverride);
                DrawingConfigController.Instance.SetGravityActivate(lineSettings.changeGravityScale);

                DrawingConfigController.Instance.SetGravity(lineSettings.gravityScaleOverride);
            }
   
        }

        /*
        /// <summary>
        /// Public method to programmatically set the line width and apply it immediately.
        /// </summary>
        public void SetLineWidth(float width)
        {
            lineWidth = width;
            if (DrawingConfigController.Instance != null)
            {
                DrawingConfigController.Instance.SetWidth(lineWidth);
            }
        }*/

        /// <summary>
        /// Public method to programmatically set the use physics flag and apply it immediately.
        /// </summary>
        public void SetUsePhysics(bool usePhysicsValue)
        {
            lineSettings.usePhysics = usePhysicsValue;
            
            if (DrawingConfigController.Instance != null)
            {
                DrawingConfigController.Instance.SetUsePhysics(lineSettings.usePhysics);
            }
        }
    }
}


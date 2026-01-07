using CustomInspector; // Required for the CustomInspector attributes
using Drawing.Managers;
using UnityEngine;

namespace Utilities.Camera.CameraShake
{
    /// <summary>
    /// Utility class to test Camera Shake profiles directly from the Unity Inspector.
    /// </summary>
    public class TestShakeProfile : MonoBehaviour
    {
        [HorizontalLine("Settings", 2, FixedColor.Gray)] // Separator for better organization

        [SerializeField]
        [ForceFill(errorMessage = "Please assign a Shake Profile to test.")] // Visual warning if field is empty
        [Tooltip("The ShakeProfile ScriptableObject to be tested.")]
        private ShakeProfile shakeProfileToTest;

        [HorizontalLine("Actions", 2, FixedColor.Gray)] // Separator for better organization

        // [HideField] hides the boolean checkbox so only the button appears.
        // [Button] creates a clickable button that invokes 'ExecuteTestShake'.
        [SerializeField]
        [HideField] 
        [Button(nameof(ExecuteTestShake), label = "Execute Test Shake", size = Size.medium, tooltip = "Triggers the camera shake event with the assigned profile.")]
        private bool someNameInInspector;
        
        /// <summary>
        /// Validates the profile and triggers the shake event via the EventManager.
        /// </summary>
        public void ExecuteTestShake()
        {
            // Ensure the flag is true when the button calls this method
            someNameInInspector = true;

            if (someNameInInspector)
            {
                if (shakeProfileToTest != null)
                {
                    // [Added] Debug Log for confirmation
                    Debug.Log($"<color=cyan>[TestShakeProfile]</color> Triggering Camera Shake with profile: <b>{shakeProfileToTest.name}</b>");
                    
                    EventManager.Instance.TriggerCameraShake(shakeProfileToTest);
                }
                else
                {
                    // Changed to LogWarning for better visibility of issues
                    Debug.LogWarning("<color=orange>[TestShakeProfile]</color> Cannot execute: No Shake Profile assigned.");
                }

                someNameInInspector = false;
            }
        }
    }
}
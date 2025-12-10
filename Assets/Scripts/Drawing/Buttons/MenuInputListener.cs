using UnityEngine;
using UnityEngine.InputSystem;

namespace Drawing.Buttons
{
    public class MenuInputListener : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MenuController menuController;
        [SerializeField] private InputActionReference toggleActionReference;

        [Header("Settings")]
        [Tooltip("If true, the menu opens while holding the button and closes on release. If false, it toggles on press.")]
        [SerializeField] private bool isHoldToOpen = false;

        private void OnEnable()
        {
            if (toggleActionReference != null && toggleActionReference.action != null)
            {
                toggleActionReference.action.Enable();

                if (isHoldToOpen)
                {
                    // Hold Mode: Listen for start (press) and cancel (release)
                    toggleActionReference.action.started += OnHoldStarted;
                    toggleActionReference.action.canceled += OnHoldCanceled;
                }
                else
                {
                    // Toggle Mode: Listen for perform (click)
                    toggleActionReference.action.performed += OnTogglePerformed;
                }
            }
        }

        private void OnDisable()
        {
            if (toggleActionReference != null && toggleActionReference.action != null)
            {
                // Unsubscribe from all to be safe and clean
                toggleActionReference.action.started -= OnHoldStarted;
                toggleActionReference.action.canceled -= OnHoldCanceled;
                toggleActionReference.action.performed -= OnTogglePerformed;
                
                toggleActionReference.action.Disable();
            }
        }

        // --- Logic for Toggle Mode ---
        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            if (menuController != null)
            {
                menuController.ToggleMenu();
            }
        }

        // --- Logic for Hold Mode ---
        private void OnHoldStarted(InputAction.CallbackContext context)
        {
            if (menuController != null)
            {
                // Explicitly open the menu
                menuController.OpenMenu(); 
                // Note: Ensure MenuController has OpenMenu(), or use ToggleMenu() if it handles state automatically
            }
        }

        private void OnHoldCanceled(InputAction.CallbackContext context)
        {
            if (menuController != null)
            {
                // Explicitly close the menu
                menuController.CloseMenu();
                // Note: Ensure MenuController has CloseMenu()
            }
        }
    }
}

/*using UnityEngine;
using UnityEngine.InputSystem;
namespace Drawing.Buttons
{
    public class MenuInputListener : MonoBehaviour
    {
        [SerializeField] private MenuController menuController;
        [SerializeField] private InputActionReference toggleActionReference;

        private void OnEnable()
        {
            if (toggleActionReference != null && toggleActionReference.action != null)
            {
                toggleActionReference.action.Enable();
                toggleActionReference.action.performed += OnTogglePerformed;
            }
        }

        private void OnDisable()
        {
            if (toggleActionReference != null && toggleActionReference.action != null)
            {
                toggleActionReference.action.performed -= OnTogglePerformed;
                toggleActionReference.action.Disable();
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            if (menuController != null)
            {
                menuController.ToggleMenu();
            }
        }
    }
}*/
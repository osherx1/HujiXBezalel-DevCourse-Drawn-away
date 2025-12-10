using UnityEngine;
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
}
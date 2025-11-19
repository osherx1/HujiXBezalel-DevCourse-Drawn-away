using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drawing.LineControl
{
    public class DeleteLine : MonoBehaviour
    {
        private bool IsErasePressed()
        {
    #if ENABLE_INPUT_SYSTEM
            if (Pen.current != null && Pen.current.tip.isPressed) return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
    #endif
    #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0)) return true;
    #endif
            return false;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!IsErasePressed()) return;
            if (!other.gameObject.CompareTag("Line")) return;
            Destroy(other.gameObject);
        }
    }
}

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drawing.Utilities
{
    // Attach this to any active GameObject to see live input state.
    public class InputDiagnostics : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private bool showOnGUI = true;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Vector2 offset = new Vector2(10, 10);

        [Header("Logs")] 
        [SerializeField] private bool logStateChanges = false;

#if ENABLE_INPUT_SYSTEM
        // Cached last state to reduce spam
        private bool _lastPenPresent;
        private bool _lastPenTip;
        private float _lastPenPressure;
        private Vector2 _lastPenPos;
        private bool _lastMousePresent;
        private bool _lastMouseLeft;
        private Vector2 _lastMousePos;

        private string BuildReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Input Diagnostics (New Input System)");

            var pen = Pen.current;
            bool penPresent = pen != null;
            sb.AppendLine($"Pen.present: {penPresent}");
            if (penPresent)
            {
                bool tip = pen.tip.isPressed;
                float pressure = 0f;
                try { pressure = pen.pressure.ReadValue(); } catch { pressure = 0f; }
                Vector2 pos = Vector2.zero;
                try { pos = pen.position.ReadValue(); } catch { pos = Vector2.zero; }
                bool inRange = false;
                try { inRange = pen.inRange != null && pen.inRange.isPressed; } catch { inRange = false; }

                sb.AppendLine($"Pen.tip: {tip}");
                sb.AppendLine($"Pen.pressure: {pressure:F2}");
                sb.AppendLine($"Pen.position: {pos}");
                sb.AppendLine($"Pen.inRange: {inRange}");

                if (logStateChanges)
                {
                    // if (penPresent != _lastPenPresent || tip != _lastPenTip || Mathf.Abs(pressure - _lastPenPressure) > 0.01f)
                    //     Debug.Log($"[InputDiagnostics] Pen tip={tip} pressure={pressure:F2} pos={pos}", this);
                    _lastPenPresent = penPresent;
                    _lastPenTip = tip;
                    _lastPenPressure = pressure;
                    _lastPenPos = pos;
                }
            }

            var mouse = Mouse.current;
            bool mousePresent = mouse != null;
            sb.AppendLine($"Mouse.present: {mousePresent}");
            if (mousePresent)
            {
                bool left = mouse.leftButton.isPressed;
                Vector2 mpos = Vector2.zero;
                try { mpos = mouse.position.ReadValue(); } catch { mpos = Vector2.zero; }
                sb.AppendLine($"Mouse.left: {left}");
                sb.AppendLine($"Mouse.position: {mpos}");

                if (logStateChanges)
                {
                    // if (mousePresent != _lastMousePresent || left != _lastMouseLeft)
                    //     Debug.Log($"[InputDiagnostics] Mouse left={left} pos={mpos}", this);
                    _lastMousePresent = mousePresent;
                    _lastMouseLeft = left;
                    _lastMousePos = mpos;
                }
            }

            var touch = Touchscreen.current;
            bool touchPresent = touch != null && touch.touches.Count > 0;
            sb.AppendLine($"Touch.present: {touchPresent}");
            if (touchPresent)
            {
                var primary = touch.touches[0];
                bool pressed = primary.press.isPressed;
                Vector2 tpos = Vector2.zero;
                try { tpos = primary.position.ReadValue(); } catch { tpos = Vector2.zero; }
                sb.AppendLine($"Touch.primaryPressed: {pressed}");
                sb.AppendLine($"Touch.position: {tpos}");
            }

            return sb.ToString();
        }
#endif

        private void OnGUI()
        {
            if (!showOnGUI) return;
#if ENABLE_INPUT_SYSTEM
            var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = textColor } };
            string report = BuildReport();
            GUI.Label(new Rect(offset.x, offset.y, Screen.width, Screen.height), report, style);
#else
            var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = textColor } };
            GUI.Label(new Rect(offset.x, offset.y, 600, 200), "Input Diagnostics requires the New Input System.", style);
#endif
        }
    }
}

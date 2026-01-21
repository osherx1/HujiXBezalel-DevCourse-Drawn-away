using UnityEngine;

namespace UI
{
    public class UICursor : MonoBehaviour
    {
        [Tooltip("Drag your UI Image’s RectTransform here")]
        public RectTransform cursorRect;

        void Awake()
        {
            // Hide the OS cursor
            Cursor.visible = false;
        }

        void Update()
        {
            if (cursorRect == null) return;
            // For Screen Space – Overlay canvases, setting position = mousePosition is enough
            cursorRect.position = Input.mousePosition;
        }
    }
}
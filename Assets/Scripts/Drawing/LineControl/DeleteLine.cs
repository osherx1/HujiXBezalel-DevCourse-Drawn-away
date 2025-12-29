using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drawing.LineControl
{
    public class DeleteLine : MonoBehaviour
    {
        [SerializeField] private List<string> tagsToDelete;

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
            // 1. Basic Checks
            if (!IsErasePressed()) return;
            if (tagsToDelete == null || tagsToDelete.Count == 0) return;

            // 2. Tag Matching
            bool isTarget = false;
            foreach (string tagToCheck in tagsToDelete)
            {
                if (other.gameObject.CompareTag(tagToCheck))
                {
                    isTarget = true;
                    break;
                }
            }

            if (!isTarget) return;

            // 3. Logic: Check for Line Component on Parent
            // GetComponentInParent searches the object itself AND its parents for the component.
            Line parentLine = other.gameObject.GetComponentInParent<Line>();

            if (parentLine != null)
            {
                // Found a Line component! Destroy the entire parent object.
                Destroy(parentLine.gameObject);
            }
            else
            {
                // No Line component found. Just destroy the specific segment we hit.
                Destroy(other.gameObject);
            }
        }
    }
}
/*
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drawing.LineControl
{
    public class DeleteLine : MonoBehaviour
    {
        [SerializeField]private List<string> tagsToDelete;

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
            if(tagsToDelete == null || tagsToDelete.Count == 0) return;
            foreach (string tagToCheck in tagsToDelete)
            {
                if (other.gameObject.CompareTag(tagToCheck))
                {
                    Destroy(other.gameObject);
                    return; 
                }
            }
        }
    }
}
*/

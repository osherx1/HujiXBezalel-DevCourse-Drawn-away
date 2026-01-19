using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A trigger target that becomes "touched" when a drawn line overlaps it.
/// By default it detects the Drawing.LineControl.Line component (recommended).
/// Optionally also filter by tag.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LineTouchTarget2D : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider2D triggerCollider;

    [Header("Filter")]
    [Tooltip("Optional: if set, the other collider must have this tag.")]
    [SerializeField] private string requiredTag;

    [Tooltip("If true, requires the other collider to belong to a Drawing.LineControl.Line (recommended).")]
    [SerializeField] private bool requireLineComponent = true;

    [Header("State (Read Only)")]
    [SerializeField] private bool touched;

    [Header("Events")]
    public UnityEvent onTouched;

    public bool IsTouched => touched;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (touched)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(requiredTag) && !other.CompareTag(requiredTag))
        {
            return;
        }

        if (requireLineComponent)
        {
            // Detect line object robustly, without needing tags.
            var line = other.GetComponentInParent<Drawing.LineControl.Line>();
            if (line == null)
            {
                return;
            }
        }

        touched = true;
        onTouched?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("LineTouch/Reset")]
    private void EditorReset()
    {
        touched = false;
    }
#endif
}

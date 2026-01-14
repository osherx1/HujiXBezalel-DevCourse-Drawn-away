using UnityEngine;

/// <summary>
/// Illana-specific bubble trigger: supports two bubble variants.
/// - Variant 0: default / first bubble
/// - Variant 1: "after escort" bubble
///
/// Uses IllanaProgressManager to decide which bubble to show.
/// </summary>
[DisallowMultipleComponent]
public class IllanaSpeechBubbleTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider2D triggerCollider;

    [Tooltip("Optional: if set, only this Transform can trigger the bubble.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Optional: used if playerTransform is null.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Character")]
    [SerializeField] private CharacterId character = CharacterId.Illana;

    [Header("Bubble Registry")]
    [SerializeField] private SpeechBubbleRegistry registry;

    [Tooltip("If true, entering this trigger will hide other bubbles first.")]
    [SerializeField] private bool hideOthersOnEnter = true;

    [Header("Progress")]
    [Tooltip("Optional: if empty, uses IllanaProgressManager.Instance.")]
    [SerializeField] private IllanaProgressManager progress;

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

        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }

        if (progress != null)
        {
            progress.MarkMet();
        }

        int variant = (progress != null && progress.EscortCompleted) ? 1 : 0;

        if (registry != null)
        {
            registry.ShowVariant(character, variant, hideOthersOnEnter);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (registry != null)
        {
            registry.Hide(character);
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (playerTransform != null)
        {
            return other.transform == playerTransform;
        }

        return !string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag);
    }
}

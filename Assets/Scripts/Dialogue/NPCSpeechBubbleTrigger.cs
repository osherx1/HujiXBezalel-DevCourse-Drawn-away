using UnityEngine;

/// <summary>
/// Trigger zone for showing/hiding a character's speech bubble.
/// Attach to an NPC (or an empty trigger object). Requires a Collider2D set as Trigger.
/// </summary>
[DisallowMultipleComponent]
public class NPCSpeechBubbleTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider2D triggerCollider;

    [Tooltip("Optional: if set, only this Transform can trigger the bubble.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Optional: used if playerTransform is null.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Character")]
    [SerializeField] private CharacterId character = CharacterId.None;

    [Header("Bubble Registry")]
    [Tooltip("Reference to the SpeechBubbleRegistry in the scene. If empty, will try to find one.")]
    [SerializeField] private SpeechBubbleRegistry registry;

    [Tooltip("If true, entering this trigger will hide other bubbles first.")]
    [SerializeField] private bool hideOthersOnEnter = true;

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

        if (registry != null)
        {
            registry.Show(character, hideOthersOnEnter);
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

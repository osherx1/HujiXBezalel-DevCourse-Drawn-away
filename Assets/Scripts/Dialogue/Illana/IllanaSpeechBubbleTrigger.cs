using UnityEngine;
using UnityEngine.Serialization;

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

    [Tooltip("Trigger used before escort is completed (typically further away).")]
    [SerializeField] private Collider2D beforeEscortTriggerCollider;

    [Tooltip("Trigger used after escort is completed (typically closer).")]
    [SerializeField] private Collider2D afterEscortTriggerCollider;

    [FormerlySerializedAs("triggerCollider")]
    [Tooltip("Legacy/fallback trigger collider. If both before/after are empty, this will be used.")]
    [SerializeField] private Collider2D defaultTriggerCollider;

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

    private void OnEnable()
    {
        EnsureProgress();

        if (progress != null)
        {
            progress.StateChanged += HandleProgressChanged;
        }

        UpdateActiveTriggerCollider();
    }

    private void OnDisable()
    {
        if (progress != null)
        {
            progress.StateChanged -= HandleProgressChanged;
        }
    }

    private void Reset()
    {
        if (beforeEscortTriggerCollider == null && afterEscortTriggerCollider == null)
        {
            defaultTriggerCollider = GetComponent<Collider2D>();
            beforeEscortTriggerCollider = defaultTriggerCollider;
        }

        SetAsTrigger(beforeEscortTriggerCollider);
        SetAsTrigger(afterEscortTriggerCollider);
        SetAsTrigger(defaultTriggerCollider);
    }

    private void Awake()
    {
        if (beforeEscortTriggerCollider == null && afterEscortTriggerCollider == null)
        {
            if (defaultTriggerCollider == null)
            {
                defaultTriggerCollider = GetComponent<Collider2D>();
            }

            beforeEscortTriggerCollider = defaultTriggerCollider;
        }

        SetAsTrigger(beforeEscortTriggerCollider);
        SetAsTrigger(afterEscortTriggerCollider);
        SetAsTrigger(defaultTriggerCollider);

        EnsureRelay(beforeEscortTriggerCollider);
        EnsureRelay(afterEscortTriggerCollider);
        EnsureRelay(defaultTriggerCollider);

        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        EnsureProgress();
        UpdateActiveTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTriggerEnter(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandleTriggerExit(other);
    }

    public void RelayTriggerEnter(Collider2D other)
    {
        HandleTriggerEnter(other);
    }

    public void RelayTriggerExit(Collider2D other)
    {
        HandleTriggerExit(other);
    }

    private void HandleTriggerEnter(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        EnsureProgress();

        progress?.MarkMet();

        int variant = (progress != null && progress.EscortCompleted) ? 1 : 0;

        if (registry != null)
        {
            registry.ShowVariant(character, variant, hideOthersOnEnter);
        }
    }

    private void HandleTriggerExit(Collider2D other)
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

    private void HandleProgressChanged(object sender, IllanaProgressManager.StateChangedEventArgs e)
    {
        UpdateActiveTriggerCollider();
    }

    private void EnsureProgress()
    {
        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }
    }

    private void UpdateActiveTriggerCollider()
    {
        bool useAfterEscort = (progress != null && progress.EscortCompleted);

        // If both are assigned, enable exactly one.
        if (beforeEscortTriggerCollider != null && afterEscortTriggerCollider != null)
        {
            beforeEscortTriggerCollider.enabled = !useAfterEscort;
            afterEscortTriggerCollider.enabled = useAfterEscort;
            return;
        }

        // If only one is assigned, keep it enabled.
        if (beforeEscortTriggerCollider != null)
        {
            beforeEscortTriggerCollider.enabled = true;
        }

        if (afterEscortTriggerCollider != null)
        {
            afterEscortTriggerCollider.enabled = true;
        }

        if (defaultTriggerCollider != null)
        {
            defaultTriggerCollider.enabled = true;
        }
    }

    private static void SetAsTrigger(Collider2D c)
    {
        if (c == null)
        {
            return;
        }

        if (!c.isTrigger)
        {
            c.isTrigger = true;
        }
    }

    private void EnsureRelay(Collider2D c)
    {
        if (c == null)
        {
            return;
        }

        // If the collider is on this GameObject, Unity will invoke our OnTrigger* directly.
        if (c.gameObject == gameObject)
        {
            return;
        }

        var relay = c.GetComponent<IllanaSpeechBubbleTriggerRelay2D>();
        if (relay == null)
        {
            relay = c.gameObject.AddComponent<IllanaSpeechBubbleTriggerRelay2D>();
        }

        relay.SetOwner(this);
    }
}

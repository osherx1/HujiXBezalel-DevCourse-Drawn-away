using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

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

    [Header("Character Collider (Safety)")]
    [Tooltip("Optional: the main collider on Illana's character body. This script will keep it enabled (never disables it).")]
    [SerializeField] private Collider2D characterBodyCollider;

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

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>(8);

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

        // If this trigger gets disabled while the player is inside a trigger collider,
        // Unity may not send OnTriggerExit2D. Ensure we don't leave the bubble stuck on.
        HideIfPlayerNotOverlappingAnyEnabledTrigger();
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
        // Only force the legacy/default collider to be a trigger if it's actually being used as the trigger.
        if (beforeEscortTriggerCollider == null && afterEscortTriggerCollider == null)
        {
            SetAsTrigger(defaultTriggerCollider);
        }

        EnsureRelay(beforeEscortTriggerCollider);
        EnsureRelay(afterEscortTriggerCollider);
        if (beforeEscortTriggerCollider == null && afterEscortTriggerCollider == null)
        {
            EnsureRelay(defaultTriggerCollider);
        }

        // Auto-detect body collider if not assigned: prefer a collider on this GameObject that is not a trigger.
        if (characterBodyCollider == null)
        {
            var colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                if (c == null)
                {
                    continue;
                }

                // Prefer a non-trigger collider as the "body".
                if (!c.isTrigger)
                {
                    characterBodyCollider = c;
                    break;
                }
            }

            // Fallback: if default trigger collider is on the character GO, treat it as body collider safety.
            if (characterBodyCollider == null && defaultTriggerCollider != null && defaultTriggerCollider.gameObject == gameObject)
            {
                characterBodyCollider = defaultTriggerCollider;
            }
        }

        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        EnsureProgress();
        UpdateActiveTriggerCollider();
    }

    private void LateUpdate()
    {
        // Safety watchdog: ensure the character's main collider never gets disabled by mistake.
        if (characterBodyCollider != null && !characterBodyCollider.enabled)
        {
            characterBodyCollider.enabled = true;
        }
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

        // Only hide if the player is not still overlapping any other enabled trigger.
        HideIfPlayerNotOverlappingAnyEnabledTrigger();
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

            // If a legacy/default collider is also assigned (often a larger area), make sure
            // it does NOT stay enabled and cause ghost enters/exits.
            if (defaultTriggerCollider != null &&
                defaultTriggerCollider != beforeEscortTriggerCollider &&
                defaultTriggerCollider != afterEscortTriggerCollider)
            {
                // Never disable the character's main body collider.
                if (defaultTriggerCollider != characterBodyCollider)
                {
                    defaultTriggerCollider.enabled = false;
                }
            }

            // Switching enabled colliders can skip OnTriggerExit2D.
            HideIfPlayerNotOverlappingAnyEnabledTrigger();
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

        HideIfPlayerNotOverlappingAnyEnabledTrigger();
    }

    private void HideIfPlayerNotOverlappingAnyEnabledTrigger()
    {
        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        if (registry == null)
        {
            return;
        }

        if (IsPlayerOverlappingEnabledTrigger(beforeEscortTriggerCollider) ||
            IsPlayerOverlappingEnabledTrigger(afterEscortTriggerCollider) ||
            IsPlayerOverlappingEnabledTrigger(defaultTriggerCollider))
        {
            return;
        }

        registry.Hide(character);
    }

    private bool IsPlayerOverlappingEnabledTrigger(Collider2D trigger)
    {
        if (trigger == null || !trigger.enabled)
        {
            return false;
        }

        _overlapResults.Clear();

        // OverlapCollider works for trigger colliders too; we explicitly allow triggers.
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false,
            useNormalAngle = false,
        };

        int count = trigger.Overlap(filter, _overlapResults);
        for (int i = 0; i < count && i < _overlapResults.Count; i++)
        {
            if (IsPlayer(_overlapResults[i]))
            {
                return true;
            }
        }

        return false;
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

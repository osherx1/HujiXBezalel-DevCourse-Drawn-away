using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a configurable list mapping CharacterId -> Speech Bubble GameObject.
/// Place one instance in the scene (e.g., under a UI Canvas) and assign bubbles.
/// </summary>
[DisallowMultipleComponent]
public class SpeechBubbleRegistry : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public CharacterId character;
        public GameObject bubble;

        [Tooltip("Optional: secondary speech bubble for this character (variant 1).")]
        public GameObject bubble2;

        [Header("Follow")]
        [Tooltip("Optional: world-space target the bubble should follow (e.g., the NPC head transform).")]
        public Transform followTarget;

        [Tooltip("World offset applied when following the target.")]
        public Vector3 followOffset = new Vector3(0f, 1.5f, 0f);
    }

    [Header("Bubbles")]
    [Tooltip("Map each character to its unique speech bubble GameObject.")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    private readonly Dictionary<CharacterId, Entry> _map = new Dictionary<CharacterId, Entry>();

    private void Awake()
    {
        RebuildMap();
        HideAll();
    }

    public void RebuildMap()
    {
        _map.Clear();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.character == CharacterId.None)
            {
                continue;
            }

            // Last one wins if duplicates exist.
            _map[e.character] = e;
        }
    }

    public void Show(CharacterId character, bool hideOthers = true)
    {
        ShowVariant(character, 0, hideOthers);
    }

    public void ShowVariant(CharacterId character, int variantIndex, bool hideOthers = true)
    {
        if (character == CharacterId.None)
        {
            return;
        }

        if (hideOthers)
        {
            HideAll();
        }

        if (!_map.TryGetValue(character, out Entry entry) || entry == null)
        {
            Debug.LogWarning($"SpeechBubbleRegistry: No bubble assigned for '{character}'.", this);
            return;
        }

        // Always ensure only ONE bubble variant is active per character.
        if (variantIndex <= 0)
        {
            HideBubbleObject(entry.bubble2);
        }
        else if (variantIndex == 1)
        {
            HideBubbleObject(entry.bubble);
        }
        else
        {
            HideBubbleObject(entry.bubble);
            HideBubbleObject(entry.bubble2);
            Debug.LogWarning($"SpeechBubbleRegistry: No bubble variant {variantIndex} assigned for '{character}'.", this);
            return;
        }

        GameObject bubble = GetVariantBubble(entry, variantIndex);
        if (bubble == null)
        {
            Debug.LogWarning($"SpeechBubbleRegistry: No bubble variant {variantIndex} assigned for '{character}'.", this);
            return;
        }

        if (!bubble.activeSelf)
        {
            bubble.SetActive(true);
        }

        // Configure world-follow using the same pattern as TargetPositionTracker (LateUpdate + offset).
        if (entry.followTarget != null)
        {
            SpeechBubbleFollower follower = bubble.GetComponent<SpeechBubbleFollower>();
            if (follower == null)
            {
                follower = bubble.AddComponent<SpeechBubbleFollower>();
            }

            follower.SetTarget(entry.followTarget);
            follower.SetOffset(entry.followOffset);
            follower.enabled = true;
        }
    }

    public void Hide(CharacterId character)
    {
        if (character == CharacterId.None)
        {
            return;
        }

        if (!_map.TryGetValue(character, out Entry entry) || entry == null)
        {
            return;
        }

        HideBubbleObject(entry.bubble);
        HideBubbleObject(entry.bubble2);
    }

    public void HideAll()
    {
        foreach (var kv in _map)
        {
            var entry = kv.Value;
            if (entry == null)
            {
                continue;
            }

            HideBubbleObject(entry.bubble);
            HideBubbleObject(entry.bubble2);
        }
    }

    private static GameObject GetVariantBubble(Entry entry, int variantIndex)
    {
        if (entry == null)
        {
            return null;
        }

        if (variantIndex <= 0)
        {
            return entry.bubble;
        }

        if (variantIndex == 1)
        {
            return entry.bubble2;
        }

        return null;
    }

    private static void HideBubbleObject(GameObject bubble)
    {
        if (bubble == null || !bubble.activeSelf)
        {
            return;
        }

        // Optional: disable the follower to stop LateUpdate work while hidden.
        var follower = bubble.GetComponent<SpeechBubbleFollower>();
        if (follower != null)
        {
            follower.enabled = false;
        }

        bubble.SetActive(false);
    }
}

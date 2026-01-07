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
        if (character == CharacterId.None)
        {
            return;
        }

        if (hideOthers)
        {
            HideAll();
        }

        if (!_map.TryGetValue(character, out Entry entry) || entry == null || entry.bubble == null)
        {
            Debug.LogWarning($"SpeechBubbleRegistry: No bubble assigned for '{character}'.", this);
            return;
        }

        GameObject bubble = entry.bubble;

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

        if (_map.TryGetValue(character, out Entry entry) && entry != null && entry.bubble != null && entry.bubble.activeSelf)
        {
            // Optional: disable the follower to stop LateUpdate work while hidden.
            var follower = entry.bubble.GetComponent<SpeechBubbleFollower>();
            if (follower != null)
            {
                follower.enabled = false;
            }

            entry.bubble.SetActive(false);
        }
    }

    public void HideAll()
    {
        foreach (var kv in _map)
        {
            var entry = kv.Value;
            var bubble = entry != null ? entry.bubble : null;
            if (bubble != null && bubble.activeSelf)
            {
                var follower = bubble.GetComponent<SpeechBubbleFollower>();
                if (follower != null)
                {
                    follower.enabled = false;
                }
                bubble.SetActive(false);
            }
        }
    }
}

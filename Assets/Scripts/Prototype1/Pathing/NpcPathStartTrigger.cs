using UnityEngine;

public class NpcPathStartTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private Transform player;

    [Header("Path")]
    [SerializeField] private NpcPath path;

    [Header("NPC")]
    [Tooltip("Prefab with NpcPathFollower + Animator. Will be spawned at the first waypoint.")]
    [SerializeField] private NpcPathFollower npcPrefab;

    [Tooltip("Optional: existing NPC in the scene (can start inactive). If set and npcPrefab is null, this NPC will be activated and started on the path.")]
    [SerializeField] private NpcPathFollower npcInScene;

    [Tooltip("If true, trigger only works once.")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

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
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (other.transform != player)
        {
            return;
        }

        hasTriggered = true;

        if (path == null || path.Waypoints == null || path.Waypoints.Count == 0)
        {
            return;
        }

        NpcPathFollower npc = null;
        if (npcPrefab != null)
        {
            npc = Instantiate(npcPrefab);
        }
        else if (npcInScene != null)
        {
            npc = npcInScene;
        }

        if (npc == null)
        {
            return;
        }

        npc.gameObject.SetActive(true);
        npc.Begin(path, player);

        if (triggerOnce && triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }
}

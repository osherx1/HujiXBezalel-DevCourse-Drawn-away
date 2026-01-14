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
        if (player == null || other == null || other.transform != player)
        {
            return;
        }

        TriggerNow(player);
    }

    public void TriggerNow()
    {
        TriggerNow(player);
    }

    public void TriggerNow(Transform playerTransform)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (playerTransform == null)
        {
            return;
        }

        player = playerTransform;
        hasTriggered = true;

        StartNpc();

        if (triggerOnce && triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    private void StartNpc()
    {
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
    }
}

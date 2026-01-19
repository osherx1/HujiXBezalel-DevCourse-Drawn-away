using UnityEngine;
using UnityEngine.UI; // For controlling buttons
using Drawing.Managers; // Assuming access to your namespaces

public class GiantBossController : MonoBehaviour
{
    [Header("--- REFERENCES ---")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform groundSpawnPoint; // Where player restarts
    
    [Header("--- PHASE HEIGHTS ---")]
    [SerializeField] private float kneesHeight = 10f;
    [SerializeField] private float shouldersHeight = 25f;
    [SerializeField] private float headHeight = 40f;

    [Header("--- ATTACK MODULES ---")]
    [SerializeField] private GiantStompMechanic stompModule;
    [SerializeField] private StalactiteSpawner stalactiteModule;
    [SerializeField] private FireballShooter fireballModule;

    [Header("--- MATERIAL STEALING ---")]
    [Tooltip("The buttons to disable at start (Iron, Cloud, Sticky).")]
    [SerializeField] private Button[] materialsToSteal;

    private int _currentPhase = 0;
    private bool _hasStolenMaterials = false;

    private void Start()
    {
        // 1. Initial Material Steal (Only happens once per scene load)
        if (!_hasStolenMaterials)
        {
            StealMaterials();
            _hasStolenMaterials = true;
        }

        ResetBossFight();
    }

    private void Update()
    {
        if (player == null) return;

        float y = player.position.y;

        // --- PHASE LOGIC (STACKING) ---
        // Phase 1: Knees reached -> Start Stomping
        if (y >= kneesHeight && _currentPhase < 1)
        {
            SetPhase(1);
        }
        // Phase 2: Shoulders reached -> Add Stalactites (Stomp stays on)
        if (y >= shouldersHeight && _currentPhase < 2)
        {
            SetPhase(2);
        }
        // Phase 3: Head reached -> Add Fireballs (Stomp + Stalactites stay on)
        if (y >= headHeight && _currentPhase < 3)
        {
            SetPhase(3);
        }
    }

    private void SetPhase(int phase)
    {
        _currentPhase = phase;
        Debug.Log($"BOSS: Entering Phase {phase}");

        // Enable mechanics cumulatively
        if (phase >= 1) stompModule.enabled = true;
        if (phase >= 2) stalactiteModule.enabled = true;
        if (phase >= 3) fireballModule.enabled = true;
    }

    // Called when a projectile hits the player
    public void PunishPlayer()
    {
        Debug.Log("BOSS: Player Hit! Resetting...");

        // 1. Teleport Player
        // Disable physics briefly to prevent collision glitches during teleport
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
        player.position = groundSpawnPoint.position;

        // 2. Reset Boss State
        ResetBossFight();
    }

    private void ResetBossFight()
    {
        _currentPhase = 0;
        
        // Disable all attacks
        if (stompModule) stompModule.enabled = false;
        if (stalactiteModule) stalactiteModule.enabled = false;
        if (fireballModule) fireballModule.enabled = false;
        
        // Note: We do NOT re-steal materials here.
    }

    private void StealMaterials()
    {
        foreach (var btn in materialsToSteal)
        {
            if (btn != null) btn.interactable = false;
        }
    }

    // Helper to unlock a specific material (Call this from your Pickup Item script)
    public void UnlockMaterial(Button materialButton)
    {
        if (materialButton != null) materialButton.interactable = true;
    }
}
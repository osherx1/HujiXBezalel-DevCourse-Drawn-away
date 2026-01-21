using UnityEngine;
using UnityEngine.UI;
using Drawing.Managers;

public class GiantBossController : MonoBehaviour
{
    [Header("--- REFERENCES ---")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform groundSpawnPoint;
    
    [Header("--- PHASE HEIGHTS ---")]
    [SerializeField] private float kneesHeight = 10f;
    [SerializeField] private float shouldersHeight = 25f;
    [SerializeField] private float headHeight = 40f;

    [Header("--- ATTACK MODULES ---")]
    [SerializeField] private GiantStompMechanic stompModule;
    [SerializeField] private StalactiteSpawner stalactiteModule;
    [SerializeField] private FireballShooter fireballModule;

    [Header("--- MATERIAL STEALING ---")]
    [SerializeField] private Button[] materialsToSteal;

    private int _currentPhase = 0;
    private bool _hasStolenMaterials = false;
    private bool _isDefeated = false;

    private void Start()
    {
        if (!_hasStolenMaterials)
        {
            StealMaterials();
            _hasStolenMaterials = true;
        }
        ResetBossFight();
    }

    private void Update()
    {
        if (_isDefeated || player == null) return;

        float y = player.position.y;

        // --- PHASE LOGIC ---
        // Note: The "Win" check is now handled by BossWinSequence.cs

        if (y >= kneesHeight && _currentPhase < 1) SetPhase(1);
        else if (y >= shouldersHeight && _currentPhase < 2) SetPhase(2);
        else if (y >= headHeight && _currentPhase < 3) SetPhase(3);
    }

    private void SetPhase(int phase)
    {
        _currentPhase = phase;
        if (phase >= 1 && stompModule) stompModule.enabled = true;
        if (phase >= 2 && stalactiteModule) stalactiteModule.enabled = true;
        if (phase >= 3 && fireballModule) fireballModule.enabled = true;
    }

    public void PunishPlayer()
    {
        if (_isDefeated) return;

        // Teleport & Reset
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
        player.position = groundSpawnPoint.position;
        ResetBossFight();
    }

    private void ResetBossFight()
    {
        _currentPhase = 0;
        if (stompModule) stompModule.enabled = false;
        if (stalactiteModule) stalactiteModule.enabled = false;
        if (fireballModule) fireballModule.enabled = false;
    }

    private void StealMaterials()
    {
        foreach (var btn in materialsToSteal)
        {
            if (btn != null) btn.interactable = false;
        }
    }

    public void UnlockMaterial(Button materialButton)
    {
        if (materialButton != null) materialButton.interactable = true;
    }

    // Called by BossWinSequence when the player reaches the top
    public void DefeatBossInternal()
    {
        _isDefeated = true;
        // Turn off all hazards immediately
        if (stompModule) stompModule.enabled = false;
        if (stalactiteModule) stalactiteModule.enabled = false;
        if (fireballModule) fireballModule.enabled = false;
    }
}
using UnityEngine;
using Drawing.LineControl;

public class StalactiteProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Effect when the rock itself hits something (Dust/Impact).")]
    [SerializeField] private GameObject impactEffect;

    [Header("Physics Settings")]
    [Tooltip("How much speed to keep after smashing a platform (0.5 = 50% speed).")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float impactSlowdown = 0.5f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Hit Player -> Punish & Destroy
        if (other.CompareTag("Player"))
        {
            var boss = FindFirstObjectByType<GiantBossController>();
            if (boss != null) boss.PunishPlayer();
            
            SpawnImpactEffect(); // Blood/Dust?
            Destroy(gameObject); 
        }
        // 2. Hit Player Drawn Line -> Destroy Line
        else if (other.TryGetComponent(out Line line))
        {
            Destroy(line.gameObject);
            SpawnImpactEffect(); // Rock dust
            ApplySlowdown();
        }
        // 3. Hit Respawning Platform -> Trigger Platform's Logic
        else if (other.TryGetComponent(out RespawningPlatform platform))
        {
            // We just tell the platform "You broke!"
            // The platform decides if it crumbles or fades.
            platform.BreakAndRespawn();
            
            SpawnImpactEffect(); // Rock dust from impact
            ApplySlowdown();
        }
        // 4. Hit Floor -> Destroy
        else if (other.CompareTag("Ground"))
        {
            SpawnImpactEffect();
            Destroy(gameObject);
        }
    }

    private void ApplySlowdown()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = _rb.linearVelocity * impactSlowdown;
        }
    }

    private void SpawnImpactEffect()
    {
        // This is the dust from the ROCK, not the platform wood
        if (impactEffect) 
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
    }
}
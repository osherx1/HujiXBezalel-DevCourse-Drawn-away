using UnityEngine;
using Drawing.LineControl; // Needed to detect Line components

public class FireballProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject explosionEffect;

    [Header("Settings")]
    [SerializeField] private string ironLineTag = "IronLine";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Hit Player -> Punish & Explode
        if (other.CompareTag("Player"))
        {
            var boss = FindFirstObjectByType<GiantBossController>();
            if (boss != null) boss.PunishPlayer();
            
            Explode(); // Destroy fireball
        }
        // 2. Hit a Drawn Line
        else if (other.TryGetComponent(out Line line))
        {
            if (line.CompareTag(ironLineTag))
            {
                // HIT IRON: Fireball dies, Line survives (Shield effect)
                Explode();
            }
            else
            {
                // HIT NORMAL: Both die
                Destroy(line.gameObject);
                Explode();
            }
        }
        
        // Note: We removed the check for "Ground" or "RespawningPlatform".
        // The fireball will now pass through them safely.
    }

    private void Explode()
    {
        if (explosionEffect) 
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [SerializeField] private GameObject explosionEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hit Player -> Reset
        if (other.CompareTag("Player"))
        {
            var boss = FindFirstObjectByType<GiantBossController>();
            if (boss != null) boss.PunishPlayer();
            
            Explode();
        }
        // Hit Wall/Platform -> Destroy
        else if (other.CompareTag("Ground") || other.GetComponent<RespawningPlatform>())
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionEffect) Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
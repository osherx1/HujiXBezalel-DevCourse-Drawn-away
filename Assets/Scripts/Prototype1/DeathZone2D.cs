using UnityEngine;

namespace Prototype1
{
    /// <summary>
    /// Generic kill volume. Put this on any Collider2D (trigger recommended).
    /// When the player enters, it triggers the same death/respawn flow as spikes.
    /// Useful for fall zones, lava, out-of-bounds areas, etc.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone2D : MonoBehaviour
    {
        [Header("Filter")]
        [SerializeField] private string requiredTag = "Player";

        [Header("Behavior")]
        [Tooltip("When true the player's velocity is cleared before respawning.")]
        [SerializeField] private bool zeroVelocityOnHit = true;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryKill(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryKill(collision.collider);
        }

        private void TryKill(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            var hurt = other.GetComponent<characterHurt>() ?? other.GetComponentInParent<characterHurt>();
            if (hurt != null)
            {
                hurt.TriggerHazardHit(zeroVelocityOnHit);
            }
        }
    }
}

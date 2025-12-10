using UnityEngine;

namespace Prototype1
{
    /// <summary>
    /// Simple hazard that sends the player back to their configured respawn point.
    /// Attach to any spike or kill-zone object with a Collider2D set as a trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SpikeHazard : MonoBehaviour
    {
        [Tooltip("Only objects with this tag are affected. Leave empty to allow any collider with a characterHurt component.")]
        [SerializeField] private string requiredTag = "Player";
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
            TryResetPlayer(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryResetPlayer(collision.collider);
        }

        private void TryResetPlayer(Collider2D other)
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

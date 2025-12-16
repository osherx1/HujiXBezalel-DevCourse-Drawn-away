using UnityEngine;

namespace Prototype1
{
    /// <summary>
    /// Place this on a checkpoint object (with a Collider2D marked as Trigger).
    /// When the player enters, it updates the player's respawn checkpoint.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [Header("Filter")]
        [SerializeField] private string requiredTag = "Player";

        [Header("Checkpoint Position")]
        [Tooltip("If set, this transform's position will be saved as the checkpoint. If empty, uses this object's transform.")]
        [SerializeField] private Transform respawnPoint;

        [Header("One-shot")]
        [Tooltip("If true, this checkpoint will only trigger once.")]
        [SerializeField] private bool triggerOnce = true;

        private bool _used;

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
            if (_used && triggerOnce)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            var hurt = other.GetComponent<characterHurt>() ?? other.GetComponentInParent<characterHurt>();
            if (hurt == null)
            {
                return;
            }

            Vector3 checkpointPosition = (respawnPoint != null ? respawnPoint.position : transform.position);
            hurt.newCheckpoint(checkpointPosition);

            _used = true;
        }
    }
}

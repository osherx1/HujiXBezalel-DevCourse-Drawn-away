using Drawing.Managers;
using UnityEngine;

namespace Prototype1
{
    /// <summary>
    /// Attach to a 2D trigger placed at the end of the level. When the player enters, fires the game-finished event.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GameEndTrigger : MonoBehaviour
    {
        [Tooltip("Objects with this tag can finish the level.")]
        [SerializeField] private string requiredTag = "Player";
        [Tooltip("Prevent multiple triggers if the player stays inside.")]
        [SerializeField] private bool triggerOnce = true;

        private bool _hasTriggered;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered && triggerOnce)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            _hasTriggered = true;
            EventManager.Instance?.TriggerGameFinished();
        }
    }
}

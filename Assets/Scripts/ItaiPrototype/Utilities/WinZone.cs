using UnityEngine;

namespace ItaiPrototype.Utilities
{
    public class WinZone : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Player"; // What needs to touch this zone?
        [SerializeField] private ScenarioManager scenarioManager;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 1. Check if the specific part hit has the tag (standard check)
            bool directHit = other.CompareTag(targetTag);

            // 2. Check if the PARENT RIGIDBODY has the tag (The Compound Collider check)
            // 'attachedRigidbody' automatically looks up the hierarchy to find the controlling RB
            bool parentHit = other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(targetTag);

            if (directHit || parentHit)
            {
                scenarioManager.Win();
            }
        }
    
        // Alternatively, for physics collisions:
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(targetTag))
            {
                scenarioManager.Win();
            }
        }
    }
}
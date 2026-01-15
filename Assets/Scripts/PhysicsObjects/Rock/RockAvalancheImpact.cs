using UnityEngine;

namespace PhysicsObjects.Rock
{
    public class RockAvalancheImpact : MonoBehaviour
    {
        [Header("Push Settings")]
        [SerializeField] private Vector2 pushDirection = new Vector2(1f, -0.5f);
        [SerializeField] private float pushForce = 15f;
        [SerializeField] private bool disableControlOnImpact = false;

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                ApplyAvalancheForce(other.gameObject);
            }
        }

        private void ApplyAvalancheForce(GameObject player)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

            if (playerRb != null)
            {
                // Reset current velocity to ensure the push is effective
                playerRb.linearVelocity = Vector2.zero;
                
                // Apply immediate force in the desired direction
                playerRb.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
            }

            // Optional: Call a method on the player controller to disable input momentarily
            if (disableControlOnImpact)
            {
                // Example: player.GetComponent<PlayerController>()?.DisableInput(1.0f);
            }
        }

        // Draw the direction in the editor to make setup easier
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, pushDirection.normalized * 2f);
        }
    }
}
using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;

namespace PhysicsObjects.Rock
{
    [RequireComponent(typeof(Collider2D))]
    public class GameEnderKicker : MonoBehaviour
    {
        [Header("Kick Settings")]
        [Tooltip("The direction and magnitude of the kick.")]
        [SerializeField] private Vector2 kickDirection = new Vector2(1f, 1f);
        
        [SerializeField] private float kickForce = 20f;
        [SerializeField] private Transform barrierTransform;
        [SerializeField] private GameObject playerObject;
        
        [Tooltip("Should we zero out the player's velocity before kicking? (Recommended for consistency)")]
        [SerializeField] private bool resetVelocityBeforeKick = true;
        
        [Header("Audio")]
        [SerializeField] private GameSoundsSo.AudioType audioType = GameSoundsSo.AudioType.None;
        [SerializeField] private float soundVolume = 1f;

        [Header("Configuration")]
        [SerializeField] private string collisionTag = "Barrier";
        [SerializeField] private bool oneTimeOnly = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true; // Toggle to turn logs on/off

        private bool _hasActivated = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (showDebugLogs) Debug.Log($"[GameEnderKicker] Trigger entered by: '{other.name}' (Tag: {other.tag})");

            if (_hasActivated && oneTimeOnly) 
            {
                if (showDebugLogs) Debug.Log("[GameEnderKicker] Aborted: Already activated.");
                return;
            }

            if (other.CompareTag(collisionTag))
            {
                if (showDebugLogs) Debug.Log($"[GameEnderKicker] Tag '{collisionTag}' matched! Initiating kick on assigned player object.");
                ActivateKick(playerObject);
            }
            else
            {
                if (showDebugLogs) Debug.Log($"[GameEnderKicker] Ignored: Tag mismatch. Expected '{collisionTag}', got '{other.tag}'.");
            }
        }

        private void ActivateKick(GameObject player)
        {
            if (player == null)
            {
                Debug.LogError("[GameEnderKicker] CRITICAL ERROR: 'Player Object' is not assigned in the Inspector!");
                return;
            }

            _hasActivated = true;

            // Handle Barrier
            if (barrierTransform != null)
            {
                barrierTransform.gameObject.SetActive(false);
                if (showDebugLogs) Debug.Log("[GameEnderKicker] Barrier disabled.");
            }
            else
            {
                Debug.LogWarning("[GameEnderKicker] Warning: 'Barrier Transform' is missing/null.");
            }

            // 1. Stop other game processes
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerDropPlayerToTheHole(true);
                if (showDebugLogs) Debug.Log("[GameEnderKicker] Event 'TriggerDropPlayerToTheHole' fired.");
            }
            else
            {
                Debug.LogError("[GameEnderKicker] Error: EventManager Instance is null.");
            }

            // 2. Play Sound
            if (audioType != GameSoundsSo.AudioType.None)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySoundByAudioType(audioType, soundVolume);
                    if (showDebugLogs) Debug.Log($"[GameEnderKicker] Sound triggered: {audioType}");
                }
                else
                {
                    Debug.LogWarning("[GameEnderKicker] Warning: AudioManager Instance is null, cannot play sound.");
                }
            }

            // 3. Apply Physical Kick
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                if (resetVelocityBeforeKick)
                {
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.angularVelocity = 0f;
                }

                // Apply instant force
                Vector2 finalForce = kickDirection.normalized * kickForce;
                playerRb.AddForce(finalForce, ForceMode2D.Impulse);
                
                if (showDebugLogs) Debug.Log($"[GameEnderKicker] SUCCESS: Player kicked with force: {finalForce}");
            }
            else
            {
                Debug.LogError($"[GameEnderKicker] Error: No Rigidbody2D found on player object '{player.name}'.");
            }
        }

        // --- Visual Aid for Editor ---
// --- Visual Aid for Editor ---
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;

            // 1. Draw the trigger box
            Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>() != null ? GetComponent<BoxCollider2D>().size : Vector3.one);

            // 2. Draw Direction Arrow (Straight line)
            Vector3 startPos = transform.position;
            Vector3 directionEnd = startPos + (Vector3)(kickDirection.normalized * 2f);
            Gizmos.DrawLine(startPos, directionEnd);
            Gizmos.DrawSphere(directionEnd, 0.1f);

            // 3. Draw Trajectory Prediction (The Curve)
            if (playerObject != null)
            {
                Rigidbody2D playerRb = playerObject.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Gizmos.color = Color.yellow; // Color for the path
                    
                    // --- Physics Calculation ---
                    // Velocity = Force / Mass (Impulse mode)
                    Vector2 initialVelocity = (kickDirection.normalized * kickForce) / playerRb.mass;
                    
                    // Gravity = Physics Gravity * Player's Gravity Scale
                    Vector2 gravity = Physics2D.gravity * playerRb.gravityScale;

                    // --- Simulation ---
                    Vector3 previousPoint = startPos;
                    float simulationDuration = 2.0f; // How many seconds to look ahead
                    int resolution = 30; // How many segments to draw
                    float timeStep = simulationDuration / resolution;

                    for (int i = 1; i <= resolution; i++)
                    {
                        float t = i * timeStep;
                        
                        // Trajectory Formula: Position = Start + (Vel * t) + (0.5 * Gravity * t^2)
                        Vector2 displacement = (initialVelocity * t) + (0.5f * gravity * (t * t));
                        Vector3 currentPoint = startPos + (Vector3)displacement;

                        // Draw line segment
                        Gizmos.DrawLine(previousPoint, currentPoint);
                        previousPoint = currentPoint;
                    }
                    
                    // Draw a small target at the end of prediction
                    Gizmos.DrawWireSphere(previousPoint, 0.2f);
                }
            }
        }
        /*private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            
            // Draw the box
            Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>() != null ? GetComponent<BoxCollider2D>().size : Vector3.one);

            // Draw the kick direction arrow
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)(kickDirection.normalized * 3f); 
            
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.2f); 
        }*/
    }
}
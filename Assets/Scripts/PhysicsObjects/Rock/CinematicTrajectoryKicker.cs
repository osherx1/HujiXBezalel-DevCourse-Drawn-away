using System.Collections;
using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using Utilities.Camera.CameraShake;

namespace PhysicsObjects.Rock
{
    public class CinematicTrajectoryKicker : MonoBehaviour
    {
        [Header("Path Settings")]
        [Tooltip("Where the player should land.")]
        [SerializeField] private Transform targetEndPoint;
        
        [Tooltip("An empty object that defines the height/curve of the path.")]
        [SerializeField] private Transform curveControlPoint;

        [Tooltip("How long (in seconds) the flight takes.")]
        [SerializeField] private float flightDuration = 1.5f;

        [Header("Impact Feel (Drama)")]
        [Tooltip("Time to wait (shaking) before the player actually flies.")]
        [SerializeField] private float preKickDelay = 1.5f; 
        
        [Tooltip("The Camera Shake profile to play during the delay.")]
        [SerializeField] private ShakeProfile shakeProfile;

        [Header("Audio")]
        [SerializeField] private GameSoundsSo.AudioType audioType = GameSoundsSo.AudioType.None;
        [SerializeField] private float soundVolume = 1f;

        [Header("Configuration")]
        [SerializeField] private string collisionTag = "Barrier";
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Transform barrierTransform;
        [SerializeField] private bool showDebugLogs = true;

        private bool _hasActivated = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasActivated) return;

            if (other.CompareTag(collisionTag))
            {
                if (showDebugLogs) Debug.Log($"[CinematicKicker] Activated by {other.name}");
                StartCoroutine(PerformCinematicSequence());
            }
        }

        private IEnumerator PerformCinematicSequence()
        {
            _hasActivated = true;

            // --- 1. Validation ---
            if (playerObject == null || targetEndPoint == null || curveControlPoint == null)
            {
                Debug.LogError("[CinematicKicker] Missing references (Player, EndPoint, or ControlPoint).");
                yield break;
            }

            // --- 2. Stop Other Systems Immediately ---
            if (EventManager.Instance != null)
            {
                // This stops inputs/spawners immediately
                EventManager.Instance.TriggerDropPlayerToTheHole(true);
            }

            // Take Control of Player Physics
            Rigidbody2D playerRb = playerObject.GetComponent<Rigidbody2D>();
            bool originalKinematicState = false;
            
        
            Vector3 startPos = playerObject.transform.position;

            if (playerRb != null)
            {
                originalKinematicState = playerRb.isKinematic;
                playerRb.linearVelocity = Vector2.zero; // Stop movement
                playerRb.isKinematic = true; // Disable physics simulation
            }

            // --- 3. Start The DRAMA (Sound + Shake) ---
            if (showDebugLogs) Debug.Log("[CinematicKicker] Starting build-up (Shake & Sound)...");

            // Play Sound
            if (audioType != GameSoundsSo.AudioType.None && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySoundByAudioType(audioType, soundVolume);
            }

            // Trigger Camera Shake via EventManager
            if (shakeProfile != null && EventManager.Instance != null)
            {
                EventManager.Instance.TriggerCameraShake(shakeProfile);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning("[CinematicKicker] No ShakeProfile assigned!");
            }


            float delayTimer = 0f;
            while (delayTimer < preKickDelay)
            {
                delayTimer += Time.deltaTime;

  
                playerObject.transform.position = startPos;
                
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector2.zero;
                }

                yield return null;
            }

            // --- 5. The Kick (Movement Logic) ---
            if (showDebugLogs) Debug.Log("[CinematicKicker] Launching Player!");

            // Disable Barrier (Open the gate/remove the trigger visual)
            if (barrierTransform != null) barrierTransform.gameObject.SetActive(false);

            // Execute Bezier Movement
    
            Vector3 endPos = targetEndPoint.position;
            Vector3 controlPos = curveControlPoint.position;
            
            float flightTimer = 0f;

            while (flightTimer < flightDuration)
            {
                flightTimer += Time.deltaTime;
                float t = flightTimer / flightDuration; // Normalized time (0 to 1)

                // Quadratic Bezier Formula
                Vector3 newPos = 
                    Mathf.Pow(1 - t, 2) * startPos + 
                    2 * (1 - t) * t * controlPos + 
                    Mathf.Pow(t, 2) * endPos;

                playerObject.transform.position = newPos;
                
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector2.zero; 
                }

                yield return null;
            }

            // --- 6. Finish ---
            // Snap to exact end
            playerObject.transform.position = endPos;
            
            // Restore Physics
            if (playerRb != null)
            {
                playerRb.isKinematic = originalKinematicState;
                // Optional: Add downward velocity to ensure they keep falling
                playerRb.linearVelocity = Vector2.down * 10f; 
            }

            if (showDebugLogs) Debug.Log("[CinematicKicker] Sequence Finished.");
        }

        // --- Editor Visualization ---
        private void OnDrawGizmos()
        {
            if (targetEndPoint == null || curveControlPoint == null) return;

            Gizmos.color = Color.cyan;
            Vector3 startPos = transform.position; // Approximation
            
            // Draw Control Points structure
            Gizmos.DrawLine(startPos, curveControlPoint.position);
            Gizmos.DrawLine(curveControlPoint.position, targetEndPoint.position);
            Gizmos.DrawWireSphere(curveControlPoint.position, 0.3f);
            Gizmos.DrawWireSphere(targetEndPoint.position, 0.3f);

            // Draw Expected Curve
            Gizmos.color = Color.yellow;
            Vector3 prevPos = startPos;
            int segments = 20;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 currentPos = 
                    Mathf.Pow(1 - t, 2) * startPos + 
                    2 * (1 - t) * t * curveControlPoint.position + 
                    Mathf.Pow(t, 2) * targetEndPoint.position;

                Gizmos.DrawLine(prevPos, currentPos);
                prevPos = currentPos;
            }
        }
    }
}
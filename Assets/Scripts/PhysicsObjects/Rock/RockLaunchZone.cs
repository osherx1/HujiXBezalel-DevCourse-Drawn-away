using System.Collections;
using UnityEngine;

namespace PhysicsObjects.Rock
{
    // 1. Changed to RequireComponent(typeof(Collider2D))
    [RequireComponent(typeof(Collider2D))]
    public class RockLaunchZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Drag the RockLauncher object here")]
        [SerializeField] private RockLauncher launcherToControl;
        
        [Tooltip("Tag of the object that activates the trap")]
        [SerializeField] private string targetTag = "Player";

        [Tooltip("Should the launcher stop when the player exits the zone?")]
        [SerializeField] private bool stopOnExit = true;

        [Header("Activation Timing")]
        [Tooltip("Time in seconds to wait from game start before this zone becomes active")]
        [SerializeField] private float activationDelay = 0f;

        // 2. Changed field type to Collider2D
        private Collider2D _zoneCollider;

        private void Awake()
        {
            _zoneCollider = GetComponent<Collider2D>();
            
            if (_zoneCollider == null)
            {
                Debug.LogError($"[{nameof(RockLaunchZone)}] Missing Collider2D component.");
                enabled = false;
                return;
            }

            // Start disabled if there is a delay
            if (activationDelay > 0)
            {
                _zoneCollider.enabled = false;
            }
        }

        private void Start()
        {
            if (activationDelay > 0)
            {
                StartCoroutine(EnableZoneRoutine());
            }
        }

        private IEnumerator EnableZoneRoutine()
        {
            yield return new WaitForSeconds(activationDelay);

            if (_zoneCollider != null)
            {
                _zoneCollider.enabled = true;
            }
        }

        // 3. Changed to OnTriggerEnter2D taking Collider2D
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (CanControlLauncher(other))
            {
                launcherToControl.SetAutoLaunch(true);
            }
        }

        // 4. Changed to OnTriggerExit2D taking Collider2D
        private void OnTriggerExit2D(Collider2D other)
        {
            if (stopOnExit && CanControlLauncher(other))
            {
                launcherToControl.SetAutoLaunch(false);
            }
        }

        private bool CanControlLauncher(Collider2D other)
        {
            return launcherToControl != null && other.CompareTag(targetTag);
        }

        private void OnValidate()
        {
            // Automatically set isTrigger for 2D collider
            if (GetComponent<Collider2D>() != null)
            {
                GetComponent<Collider2D>().isTrigger = true;
            }
        }
    }
}
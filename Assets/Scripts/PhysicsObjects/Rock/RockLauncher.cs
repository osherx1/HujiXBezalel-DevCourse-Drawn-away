using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Physics.Rock
{
    public class RockLauncher : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference launchAction;

        [Header("Configuration")]
        [SerializeField] private bool useTrapdoor = true; 
        [Tooltip("Cooldown time when trapdoor is disabled")]
        [SerializeField] private float simpleCooldown = 0.5f;

        [Header("References")]
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private TrapdoorController trapdoor;

        [Header("Trapdoor Timing")]
        [SerializeField] private float delayBeforeOpening = 1.0f;
        [SerializeField] private float openDuration = 2.0f;

        private bool _isLaunching;

        private void OnEnable()
        {
            if (launchAction != null)
            {
                launchAction.action.Enable();
                launchAction.action.performed += OnLaunchPerformed;
            }
        }

        private void OnDisable()
        {
            if (launchAction != null)
            {
                launchAction.action.performed -= OnLaunchPerformed;
                launchAction.action.Disable();
            }
        }

        private void OnLaunchPerformed(InputAction.CallbackContext context)
        {
            if (!_isLaunching)
            {
                StartCoroutine(LaunchRoutine());
            }
        }

        private IEnumerator LaunchRoutine()
        {
            _isLaunching = true;

            // 1. Handle Trapdoor Logic (Close first)
            if (useTrapdoor && trapdoor != null)
            {
                trapdoor.Close();
            }

            // 2. Spawn the rock
            SpawnRock();

            // 3. Branch Logic
            if (useTrapdoor && trapdoor != null)
            {
                // --- Trapdoor Sequence ---
                yield return new WaitForSeconds(delayBeforeOpening);
                trapdoor.Open();
                yield return new WaitForSeconds(openDuration);
                trapdoor.Close();
            }
            else
            {
                // --- Simple Cooldown Sequence ---
                // Just wait a bit so the player can't spam instantiate
                yield return new WaitForSeconds(simpleCooldown);
            }

            _isLaunching = false;
        }

        private void SpawnRock()
        {
            if (rockPrefab != null && spawnPoint != null)
            {
                Instantiate(rockPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                Debug.LogWarning("RockLauncher: Missing Prefab or SpawnPoint assignment.");
            }
        }
    }
}
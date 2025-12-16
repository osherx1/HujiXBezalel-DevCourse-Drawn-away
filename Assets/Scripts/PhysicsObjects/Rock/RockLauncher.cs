using System.Collections;
using Physics.Rock;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsObjects.Rock
{
    public class RockLauncher : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference launchAction;

        [Header("Auto Launch Settings")]
        [SerializeField] private bool enableAutoLaunch = false;
        [SerializeField] private float autoLaunchInterval = 3.0f;

        [Header("Configuration")]
        [SerializeField] private bool useTrapdoor = true; 
        [SerializeField] private float simpleCooldown = 0.5f;

        [Header("References")]
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private TrapdoorController trapdoor;

        [Header("Trapdoor Timing")]
        [SerializeField] private float delayBeforeOpening = 1.0f;
        [SerializeField] private float openDuration = 2.0f;

        private bool _isLaunching;
        private Coroutine _autoLaunchCoroutine;

        private void OnEnable()
        {
            if (launchAction != null)
            {
                launchAction.action.Enable();
                launchAction.action.performed += OnLaunchPerformed;
            }

            // Start automatically only if the bool is checked in Inspector
            if (enableAutoLaunch)
            {
                StartAutoLaunch();
            }
        }

        private void OnDisable()
        {
            if (launchAction != null)
            {
                launchAction.action.performed -= OnLaunchPerformed;
                launchAction.action.Disable();
            }

            StopAutoLaunch();
        }

        // --- Public API for Triggers ---
        public void SetAutoLaunch(bool isActive)
        {
            enableAutoLaunch = isActive;

            if (isActive)
                StartAutoLaunch();
            else
                StopAutoLaunch();
        }

        private void StartAutoLaunch()
        {
            if (_autoLaunchCoroutine == null)
            {
                _autoLaunchCoroutine = StartCoroutine(AutoLaunchLoop());
            }
        }

        private void StopAutoLaunch()
        {
            if (_autoLaunchCoroutine != null)
            {
                StopCoroutine(_autoLaunchCoroutine);
                _autoLaunchCoroutine = null;
            }
        }
        // -------------------------------

        private void OnLaunchPerformed(InputAction.CallbackContext context)
        {
            TryLaunch();
        }

        private IEnumerator AutoLaunchLoop()
        {
            // Initial delay (optional) or immediate start
            yield return null; 

            while (enableAutoLaunch)
            {
                TryLaunch();
                yield return new WaitForSeconds(autoLaunchInterval);
            }
        }

        private void TryLaunch()
        {
            if (!_isLaunching)
            {
                StartCoroutine(LaunchRoutine());
            }
        }

        private IEnumerator LaunchRoutine()
        {
            _isLaunching = true;

            if (useTrapdoor && trapdoor != null) trapdoor.Close();

            SpawnRock();

            if (useTrapdoor && trapdoor != null)
            {
                yield return new WaitForSeconds(delayBeforeOpening);
                trapdoor.Open();
                yield return new WaitForSeconds(openDuration);
                trapdoor.Close();
            }
            else
            {
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
        }
    }
}
/*using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Physics.Rock
{
    public class RockLauncher : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference launchAction;

        [Header("Auto Launch Settings")]
        [SerializeField] private bool enableAutoLaunch = false;
        [SerializeField] private float autoLaunchInterval = 3.0f;

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
        private Coroutine _autoLaunchCoroutine;

        private void OnEnable()
        {
            // Input Setup
            if (launchAction != null)
            {
                launchAction.action.Enable();
                launchAction.action.performed += OnLaunchPerformed;
            }

            // Auto Launch Setup
            if (enableAutoLaunch)
            {
                _autoLaunchCoroutine = StartCoroutine(AutoLaunchLoop());
            }
        }

        private void OnDisable()
        {
            // Input Cleanup
            if (launchAction != null)
            {
                launchAction.action.performed -= OnLaunchPerformed;
                launchAction.action.Disable();
            }

            // Auto Launch Cleanup
            if (_autoLaunchCoroutine != null)
            {
                StopCoroutine(_autoLaunchCoroutine);
                _autoLaunchCoroutine = null;
            }
        }

        private void OnLaunchPerformed(InputAction.CallbackContext context)
        {
            TryLaunch();
        }

        private IEnumerator AutoLaunchLoop()
        {
            while (enableAutoLaunch)
            {
                yield return new WaitForSeconds(autoLaunchInterval);
                TryLaunch();
            }
        }

        private void TryLaunch()
        {
            if (!_isLaunching)
            {
                StartCoroutine(LaunchRoutine());
            }
        }

        private IEnumerator LaunchRoutine()
        {
            _isLaunching = true;

            if (useTrapdoor && trapdoor != null)
            {
                trapdoor.Close();
            }

            SpawnRock();

            if (useTrapdoor && trapdoor != null)
            {
                yield return new WaitForSeconds(delayBeforeOpening);
                trapdoor.Open();
                yield return new WaitForSeconds(openDuration);
                trapdoor.Close();
            }
            else
            {
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
                Debug.LogWarning($"[{nameof(RockLauncher)}] Missing Prefab or SpawnPoint assignment.");
            }
        }
    }
}
/*using System.Collections;
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
}#1#*/
namespace PhysicsObjects.Rock
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Drawing.Managers;

    namespace Mechanics.Triggers
    {
        public class DelayedExitActivator : MonoBehaviour
        {
            [Header("Settings")]
            [Tooltip("Time in seconds to wait from the event trigger until activation.")]
            [SerializeField] private float delayTime = 10f;

            [Header("Targets")]
            [Tooltip("List of objects to enable (SetActive true) after the delay.")]
            [SerializeField] private List<GameObject> objectsToActivate;

            private void OnEnable()
            {
                // Subscribe to the event when script is enabled
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.OnDropPlayerToTheHole += HandleDropEvent;
                }
            }

            private void OnDisable()
            {
                // Unsubscribe when script is disabled to prevent memory leaks
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.OnDropPlayerToTheHole -= HandleDropEvent;
                }
            }

            // The event expects a bool parameter (Action<bool>), so the method signature must match.
            private void HandleDropEvent(bool shouldDrop)
            {
                // We verify the boolean is true before starting the timer
                if (shouldDrop)
                {
                    StartCoroutine(ActivationRoutine());
                }
            }

            private IEnumerator ActivationRoutine()
            {
                // Wait for the defined delay
                yield return new WaitForSeconds(delayTime);

                // Activate all referenced objects
                foreach (GameObject obj in objectsToActivate)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }
        }
    }
}
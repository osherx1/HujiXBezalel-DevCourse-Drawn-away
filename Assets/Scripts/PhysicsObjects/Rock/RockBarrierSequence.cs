using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhysicsObjects.Rock
{
    [RequireComponent(typeof(Collider2D))]
    public class RockBarrierSequence : MonoBehaviour
    {
        [System.Serializable]
        public struct SequenceStep
        {
            public string stepName; // For editor clarity only
            public Transform spawnPoint;
            public GameObject rockPrefabOverride; // Optional: Leave empty to use default
            public float delayBeforeSpawn;
        }

        [Header("Configuration")]
        [SerializeField] private GameObject defaultRockPrefab;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private bool destroyTriggerAfterUse = true;

        [Header("Sequence Steps")]
        [SerializeField] private List<SequenceStep> sequenceSteps = new List<SequenceStep>();

        private bool _hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered || !other.CompareTag(targetTag)) return;

            _hasTriggered = true;
            StartCoroutine(PlaySequenceRoutine());
        }

        private IEnumerator PlaySequenceRoutine()
        {
            foreach (var step in sequenceSteps)
            {
                // 1. Wait for the specified delay
                if (step.delayBeforeSpawn > 0)
                {
                    yield return new WaitForSeconds(step.delayBeforeSpawn);
                }

                // 2. Determine which prefab to use
                GameObject prefabToSpawn = step.rockPrefabOverride != null ? step.rockPrefabOverride : defaultRockPrefab;

                // 3. Spawn the rock
                if (prefabToSpawn != null && step.spawnPoint != null)
                {
                    Instantiate(prefabToSpawn, step.spawnPoint.position, step.spawnPoint.rotation);
                }
                else
                {
                    Debug.LogWarning($"[RockBarrierSequence] Missing prefab or spawn point in step: {step.stepName}");
                }
            }

            // Cleanup
            if (destroyTriggerAfterUse)
            {
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            // Visualize connections in Editor
            Gizmos.color = Color.yellow;
            foreach (var step in sequenceSteps)
            {
                if (step.spawnPoint != null)
                {
                    Gizmos.DrawLine(transform.position, step.spawnPoint.position);
                    Gizmos.DrawWireSphere(step.spawnPoint.position, 0.3f);
                }
            }
        }
    }
}
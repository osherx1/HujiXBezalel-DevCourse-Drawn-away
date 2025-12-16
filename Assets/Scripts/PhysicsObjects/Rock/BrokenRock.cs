using System;
using UnityEngine;

namespace PhysicsObjects.Rock
{
    public class BrokenRock : MonoBehaviour
    {
        [Header("Lifecycle Settings")]
        [Tooltip("Time in seconds before the rock destroys itself automatically (if not broken). Set to 0 to disable.")]
        [SerializeField] private float maxLifeTime = 5.0f; // New: Self destruct timer for the main rock
        private void Start()
        {
            // Logic: Schedule self-destruction for the main rock
            if (maxLifeTime > 0)
            {
                Destroy(gameObject, maxLifeTime);
            }
        }
    }
}
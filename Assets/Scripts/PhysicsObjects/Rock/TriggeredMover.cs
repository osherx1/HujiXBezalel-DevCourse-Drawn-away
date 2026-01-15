using System;
using UnityEngine;

namespace PhysicsObjects.Movers
{
    [RequireComponent(typeof(Collider2D))]
    public class RemoteMoverTrigger : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The actual object that should move (e.g., The Rock/Door).")]
        [SerializeField] private Transform objectToMove;

        [Tooltip("Where the object should end up (Point B).")]
        [SerializeField] private Transform destinationPoint;

        [Header("Settings")]
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private string triggeringTag = "Player";
        [SerializeField] private bool oneTimeOnly = true;

        private bool _shouldMove = false;
        private bool _hasFinished = false;

        private void Start()
        {
            // Validate references
            if (objectToMove == null)
            {
                Debug.LogError($"[{nameof(RemoteMoverTrigger)}] Missing reference to 'objectToMove'.");
            }

            if (destinationPoint == null)
            {
                Debug.LogError($"[{nameof(RemoteMoverTrigger)}] Missing reference to 'destinationPoint'.");
            }
            objectToMove.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Only run logic if triggered and not finished
            if (_shouldMove && !_hasFinished)
            {
                MoveObjectToDestination();
            }
        }

        private void MoveObjectToDestination()
        {
            if (objectToMove == null || destinationPoint == null) return;

            // Move the referenced object towards the destination
            float step = moveSpeed * Time.deltaTime;
            objectToMove.position = Vector3.MoveTowards(objectToMove.position, destinationPoint.position, step);

            // Check if reached destination
            if (Vector3.Distance(objectToMove.position, destinationPoint.position) < 0.001f)
            {
                objectToMove.position = destinationPoint.position; // Snap to exact spot
                _hasFinished = true;
                _shouldMove = false; // Stop updating
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasFinished && oneTimeOnly) return;
            

            if (other.CompareTag(triggeringTag))
            {
                objectToMove.gameObject.SetActive(true);
                _shouldMove = true;
            }
        }

        // Visual Aid: Shows which trigger moves which object to where
        private void OnDrawGizmos()
        {
            if (objectToMove != null && destinationPoint != null)
            {
                // Draw line from Trigger to Object (Yellow)
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, objectToMove.position);

                // Draw line from Object to Destination (Green)
                Gizmos.color = Color.green;
                Gizmos.DrawLine(objectToMove.position, destinationPoint.position);
                Gizmos.DrawWireSphere(destinationPoint.position, 0.3f);
            }
        }
    }
}
/*using UnityEngine;

namespace PhysicsObjects.Movers
{
    [RequireComponent(typeof(Collider2D))]
    public class TriggeredMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("The empty GameObject acting as the destination point.")]
        [SerializeField] private Transform targetDestination;
        
        [SerializeField] private float moveSpeed = 2.0f;
        
        [Header("Trigger Settings")]
        [SerializeField] private string activationTag = "Player";
        [SerializeField] private bool oneTimeOnly = true;

        private bool _isMoving = false;
        private bool _hasReachedDestination = false;

        private void Update()
        {
            // Only move if triggered and destination exists
            if (_isMoving && !_hasReachedDestination && targetDestination != null)
            {
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            // Calculate step based on speed and time
            float step = moveSpeed * Time.deltaTime;

            // Move position closer to target
            transform.position = Vector3.MoveTowards(transform.position, targetDestination.position, step);

            // Check if we reached the target (very small distance remaining)
            if (Vector3.Distance(transform.position, targetDestination.position) < 0.001f)
            {
                transform.position = targetDestination.position; // Snap to exact position
                _hasReachedDestination = true;
                _isMoving = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasReachedDestination && oneTimeOnly) return;

            if (other.CompareTag(activationTag))
            {
                _isMoving = true;
            }
        }

        // Visualize the path in the Editor
        private void OnDrawGizmos()
        {
            if (targetDestination != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetDestination.position);
                Gizmos.DrawWireSphere(targetDestination.position, 0.2f);
            }
        }
    }
}*/
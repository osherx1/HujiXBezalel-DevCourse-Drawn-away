using System.Collections.Generic;
using UnityEngine;

namespace ItaiPrototype
{
    public class CompositeGravityBody : MonoBehaviour
    {
        // We create a small class to store the data for each line segment
        [System.Serializable]
        public class GravityPoint
        {
            public Vector3 localPosition; // Where is this line relative to the center?
            public float mass;            // How heavy is it?
            public float gravityScale;    // Does it fall up, down, or float?
        }

        public List<GravityPoint> gravityPoints = new List<GravityPoint>();
        private Rigidbody2D _rb;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            // CRITICAL: We turn off standard gravity because we are handling it manually
            _rb.gravityScale = 0; 
        }

        private void FixedUpdate()
        {
            // Apply gravity for each internal part individually
            foreach (var point in gravityPoints)
            {
                // 1. Convert the local saved position to actual World space
                Vector3 worldPos = transform.TransformPoint(point.localPosition);

                // 2. Calculate gravity force: F = mass * gravity * scale
                // Physics2D.gravity is usually (0, -9.81)
                Vector2 gravityForce = Physics2D.gravity * (point.gravityScale * point.mass);

                // 3. Apply this force at that SPECIFIC spot
                _rb.AddForceAtPosition(gravityForce, worldPos);
            }
        }
        
        // Add this inside CompositeGravityBody.cs

        void OnDrawGizmos()
        {
            // Only draw if the game is running and we have points
            if (!Application.isPlaying || gravityPoints == null) return;

            Gizmos.color = Color.red;

            foreach (var point in gravityPoints)
            {
                // Calculate the world position of the gravity point
                Vector3 worldPos = transform.TransformPoint(point.localPosition);
        
                // Draw a line representing the gravity force vector
                // We multiply by 0.5f just to make the line a reasonable visual length
                Vector3 direction = Physics2D.gravity * point.gravityScale * 0.5f;
        
                Gizmos.DrawRay(worldPos, direction);
                Gizmos.DrawSphere(worldPos, 0.05f); // Draw a dot at the center of mass
            }
        }
    }
}
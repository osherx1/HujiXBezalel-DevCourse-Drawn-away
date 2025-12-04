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
    }
}
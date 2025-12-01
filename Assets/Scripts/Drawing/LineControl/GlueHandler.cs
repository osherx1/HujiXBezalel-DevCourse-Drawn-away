using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Drawing.LineControl
{
    /// <summary>
    /// attach this component to the Glue Line Prefab.
    /// It handles the sticky behavior and physics properties unique to glue.
    /// </summary>
    [RequireComponent(typeof(Line))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GlueHandler : MonoBehaviour
    {
        [Header("Glue Physics Settings")]
        [SerializeField] private float linearDrag = 5f;
        [SerializeField] private float angularDrag = 5f;
        [SerializeField] private float breakForce = 500f; // Force needed to break the bond

        //[SerializeField] private string playerTag = "Player";
        //[SerializeField] private string lineTag = "Line";
     

        [Header("Collision Filters")]
        [Tooltip("Add tags here (e.g. 'Ground' or 'Line'). The glue will only stick to objects with these tags.")]
        [SerializeField] private List<string> stickyTags;
        [SerializeField] private bool stickToTriggers = false;
        private Rigidbody2D _rb;
        private bool _isInitialized = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            // Apply "Gooey" physics settings immediately
            if (_rb != null)
            {
                _rb.linearDamping = linearDrag;
                _rb.angularDamping = angularDrag;
            }
            _isInitialized = true;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!_isInitialized) return;
            
            // 1. Filter out objects we shouldn't stick to
           // if (other.gameObject.CompareTag(playerTag)) return;
            if (!stickToTriggers && other.collider.isTrigger) return;
            
            // Check if the object even has physics (optional, depends on game design)
            // We allow sticking to static geometry (null rigidbody) or dynamic objects
            if(stickyTags.Contains(other.gameObject.tag))
            {
                CreateJoint(other.rigidbody);
            }
        }

        private void CreateJoint(Rigidbody2D otherRb)
        {
            // 2. Check if we are already connected to this body to prevent duplicate joints
            var existingJoints = GetComponents<FixedJoint2D>();
            if (otherRb != null)
            {
                if (existingJoints.Any(j => j.connectedBody == otherRb)) return;
            }
            
            // 3. Add the joint
            FixedJoint2D joint = gameObject.AddComponent<FixedJoint2D>();
            
            // 4. Configure the joint
            joint.connectedBody = otherRb; // If null, it sticks to the world position (background)
            joint.dampingRatio = 1f; // No oscillation (solid stick)
            joint.frequency = 0f;    // Rigid connection
            joint.breakForce = breakForce; //allow it to break under heavy load
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Drawing.LineControl
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GlueHandler : MonoBehaviour
    {
        [Header("General Glue Settings")]
        [SerializeField] private float linearDrag = 0.5f; // Lowered for realism
        [SerializeField] private float angularDrag = 0.05f;
        [Tooltip("Force needed to break the bond. Lower = weaker glue.")]
        [SerializeField] private float breakForce = 250f; // Drastically lowered

        [Header("Rope Specific Settings")]
        [SerializeField] private float ropeBreakForce = 150f; // Ropes snap easier

        [Header("Collision Filters")]
        [SerializeField] private List<string> stickyTags; 
        // HashSet is more performant than List.Contains for physics loops
        private HashSet<string> _stickyTagsHash; 
        
        private Rigidbody2D _rb;
        private bool _isInitialized = false;

        // Tracks unique bodies attached
        private HashSet<int> _connectedBodyIDs = new HashSet<int>();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Cache tags for performance
            _stickyTagsHash = new HashSet<string>(stickyTags);
        }

        private void Start()
        {
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
            
            // Fast tag check using Hash
            if (!_stickyTagsHash.Contains(other.gameObject.tag)) return;

            // 1. Check if it is a Rope Segment
            if (other.gameObject.TryGetComponent<RopeSegment>(out var ropeSegment))
            {
                if (IsBodyConnected(other.rigidbody)) return;
                
                // Connect at the specific point of impact for realism
                Vector2 contactPoint = other.GetContact(0).point;
                CreateJoint(other.rigidbody, ropeBreakForce, contactPoint);
            }
            // 2. Fallback for Static Lines
            else
            {
                int instanceId = other.gameObject.GetInstanceID();
                if (_connectedBodyIDs.Contains(instanceId)) return;
                
                Vector2 contactPoint = other.GetContact(0).point;
                CreateJoint(other.rigidbody, breakForce, contactPoint, instanceId);
            }
        }

        private bool IsBodyConnected(Rigidbody2D body)
        {
            if (body == null) return false;
            return _connectedBodyIDs.Contains(body.GetInstanceID());
        }

        private void CreateJoint(Rigidbody2D otherRb, float chosenBreakForce, Vector2 anchorWorldPos, int explicitInstanceID = 0)
        {
            FixedJoint2D joint = gameObject.AddComponent<FixedJoint2D>();
            
            joint.connectedBody = otherRb;
            joint.autoConfigureConnectedAnchor = false;
            
            // Anchor the joint exactly where they collided
            joint.anchor = transform.InverseTransformPoint(anchorWorldPos);
            if (otherRb != null)
            {
                joint.connectedAnchor = otherRb.transform.InverseTransformPoint(anchorWorldPos);
            }
            else
            {
                // Attaching to static world geometry
                joint.connectedAnchor = anchorWorldPos; 
            }
            
            joint.dampingRatio = 0.5f; // reduced from 0.9 to allow some shock absorption before breaking
            joint.frequency = 10f; // Rigid connection but not instant

            // 1. Calculate a Force Multiplier based on Mass
            float massMultiplier = 1f;
            if (otherRb != null)
            {
                // If it's a heavy line, we multiply the strength
                // We use a minimum of 1.0 mass to prevent dividing by tiny numbers
                massMultiplier = Mathf.Max(1f, otherRb.mass);
            }

            // 2. Apply the scaled force
            // Example: If Mass is 5, BreakForce becomes 5000 * 5 = 25,000
            joint.breakForce = chosenBreakForce * massMultiplier;

            // 3. IMPORTANT: Configure Torque
            // Rigid lines exert massive torque. If the line twists, it might break.
            // For "Tight" glue, we usually want infinite torque strength so it only breaks on pulling.
            joint.breakTorque = Mathf.Infinity;


            if (otherRb != null)
                _connectedBodyIDs.Add(otherRb.GetInstanceID());
            else if (explicitInstanceID != 0)
                _connectedBodyIDs.Add(explicitInstanceID);
        }

        private void OnJointBreak2D(Joint2D brokenJoint)
        {
            if (brokenJoint.connectedBody != null)
            {
                _connectedBodyIDs.Remove(brokenJoint.connectedBody.GetInstanceID());
            }
            // Optional: Destroy the component to clean up inspector
            // Destroy(brokenJoint); // Be careful modifying collection while iterating
        }
    }
}

/*using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Drawing.LineControl
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GlueHandler : MonoBehaviour
    {
        [Header("General Glue Settings")]
        [SerializeField] private float linearDrag = 5f;
        [SerializeField] private float angularDrag = 5f;
        [Tooltip("Standard force needed to break the bond.")]
        [SerializeField] private float breakForce = 5000f; 

        [Header("Rope Specific Settings")]
        [SerializeField] private string ropeTag = "Rope";
        [Tooltip("Force used only for ropes.")]
        [SerializeField] private float ropeBreakForce = 3500f; 

        [Header("Collision Filters")]
        [SerializeField] private List<string> stickyTags;
        [SerializeField] private bool stickToTriggers = false;
        
        private Rigidbody2D _rb;
        private bool _isInitialized = false;

        // Tracks unique bodies attached to prevent duplicate joints on same segment
        private HashSet<int> _connectedBodyIDs = new HashSet<int>();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
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
            if (!stickToTriggers && other.collider.isTrigger) return;
            
            // Fast tag check
            if(!stickyTags.Contains(other.gameObject.tag)) return;

            // 1. Check if it is a Rope Segment (Optimized)
            if (other.gameObject.TryGetComponent<RopeSegment>(out var ropeSegment))
            {
                // We do NOT block multiple connections from the same RopeID.
                // We only ensure we don't glue the *same segment* twice.
                if (IsBodyConnected(other.rigidbody)) return;

                CreateJoint(other.rigidbody, ropeBreakForce);
            }
            // 2. Fallback for Static Lines or non-rope sticky objects
            else
            {
                 // Prevent double sticking to the same static object
                 int instanceId = other.gameObject.GetInstanceID();
                 if(_connectedBodyIDs.Contains(instanceId)) return;

                 CreateJoint(other.rigidbody, breakForce, instanceId);
            }
        }

        private bool IsBodyConnected(Rigidbody2D body)
        {
            if (body == null) return false;
            return _connectedBodyIDs.Contains(body.GetInstanceID());
        }

        private void CreateJoint(Rigidbody2D otherRb, float chosenBreakForce, int explicitInstanceID = 0)
        {
            FixedJoint2D joint = gameObject.AddComponent<FixedJoint2D>();
            
            // If otherRb is null (Static geometry), FixedJoint connects to World Space at current location.
            // This is valid for static walls.
            joint.connectedBody = otherRb; 
            joint.dampingRatio = 0.9f; 
            joint.frequency = 0f;    
            joint.breakForce = chosenBreakForce; 

            // Track the connection
            if (otherRb != null)
            {
                _connectedBodyIDs.Add(otherRb.GetInstanceID());
            }
            else if (explicitInstanceID != 0)
            {
                // Track the GameObject ID for static objects without Rigidbodies
                _connectedBodyIDs.Add(explicitInstanceID);
            }
        }

        private void OnJointBreak2D(Joint2D brokenJoint)
        {
            // When a joint breaks, we must remove the ID from the set so it can stick again if needed
            if (brokenJoint.connectedBody != null)
            {
                _connectedBodyIDs.Remove(brokenJoint.connectedBody.GetInstanceID());
            }
            // Note: If connectedBody was null (static), we can't easily retrieve the ID 
            // of the object it was attached to from the broken joint alone. 
            // However, typically you don't re-stick to the exact same spot on a static wall instantly.
            // If strictly necessary, a Dictionary<Joint2D, int> mapping can be restored here.
        }
    }
}*/

/*using System.Collections.Generic;
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
            /*joint.dampingRatio = 1f; // No oscillation (solid stick)
            joint.frequency = 0f;    // Rigid connection#1#
            joint.dampingRatio = 0.8f; 
            joint.frequency = 1f;
            joint.breakForce = breakForce; //allow it to break under heavy load
        }
    }
}*/
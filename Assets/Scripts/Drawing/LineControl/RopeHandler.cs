using System.Collections.Generic;
using UnityEngine;

namespace Drawing.LineControl
{ 
    // Lightweight component to cache parent data on segments
    public class RopeSegment : MonoBehaviour
    {
        public int RopeId { get; private set; }
        public Line ParentLine { get; private set; }

        public void Initialize(Line line)
        {
            ParentLine = line;
            // Cache the ID to avoid accessing the object later if it's destroyed
            RopeId = line.GetInstanceID(); 
        }
    }
    [System.Serializable]
    public class RopeSettings
    {
        [Min(0.01f)] public float segmentLength = 0.5f;
        [Min(0.01f)] public float segmentMass = 0.2f;
        [Min(0.01f)] public float ropeWidth = 0.2f;
        public PhysicsMaterial2D physicsMaterial;
        public bool freezeRotation = true;
        [Tooltip("If true, the root of the rope is locked in space. If false, it falls attached to the parent.")]
        public bool anchorIsStatic = true;
    }


    public static class RopeBuilder
    {
        // Must match the Layer Name in your project settings
        private const string ROPE_LAYER_NAME = "Rope"; 

        public static List<Transform> Build(Transform parent, List<Vector2> points, RopeSettings settings, Line lineComponent)
        {
            var segments = new List<Transform>();
            if (parent == null || settings == null || points == null || points.Count < 2)
                return segments;

            // 1. Create Anchor
            Vector2 lastSpawnedPos = parent.TransformPoint(points[0]);
            Rigidbody2D previousRb = CreateSegment(parent, lastSpawnedPos, settings, true, segments, lineComponent, true);
            if (!previousRb) return segments;

            // 2. Iterate points
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 targetPoint = parent.TransformPoint(points[i]);
                Vector2 directionToTarget = targetPoint - lastSpawnedPos;
                float distanceToTarget = directionToTarget.magnitude;

                // 3. Fill logic
                while (distanceToTarget >= settings.segmentLength)
                {
                    Vector2 direction = directionToTarget / distanceToTarget;
                    Vector2 spawnPos = lastSpawnedPos + (direction * settings.segmentLength);

                    // Create dynamic segment
                    previousRb = CreateConnectedSegment(parent, spawnPos, settings, previousRb, segments, lineComponent);

                    lastSpawnedPos = spawnPos;
                    directionToTarget = targetPoint - lastSpawnedPos;
                    distanceToTarget = directionToTarget.magnitude;
                }
            }
            return segments;
        }

        // Helper to create the actual GO and RB
        private static Rigidbody2D CreateSegment(Transform parent, Vector2 position, RopeSettings settings, bool isAnchor, List<Transform> segments, Line lineComponent, bool isFirst = false)
        {
            GameObject segObj = new GameObject($"RopeSeg_{segments.Count}");
            segObj.transform.position = position;
            segObj.transform.SetParent(parent);
            
            // Set Layer to avoid self-collision (Requires 'Rope' layer to exist)
            int layerID = LayerMask.NameToLayer(ROPE_LAYER_NAME);
            if (layerID > -1) segObj.layer = layerID;

            // Add Collider
            CapsuleCollider2D col = segObj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(settings.ropeWidth, settings.segmentLength);
            col.direction = CapsuleDirection2D.Vertical; // Aligns with local Y usually, might need rotation logic depending on sprite
            if (settings.physicsMaterial != null) col.sharedMaterial = settings.physicsMaterial;

            // Add Rigidbody
            Rigidbody2D rb = segObj.AddComponent<Rigidbody2D>();
            rb.mass = settings.segmentMass;
            rb.freezeRotation = settings.freezeRotation;
            
            // *** CRITICAL FIX FOR TUNNELING ***
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 

            // Add Tag for GlueHandler
            segObj.tag = "Rope"; // Ensure this tag exists in Project Settings

            // Add Identity Component
            RopeSegment segmentId = segObj.AddComponent<RopeSegment>();
            segmentId.Initialize(lineComponent);

            // If it's the anchor (start of line)
            if (isAnchor && isFirst)
            {
                if (settings.anchorIsStatic)
                    rb.bodyType = RigidbodyType2D.Static;
                else
                    rb.bodyType = RigidbodyType2D.Dynamic; // Or Kinematic attached to parent
            }

            segments.Add(segObj.transform);
            return rb;
        }

        private static Rigidbody2D CreateConnectedSegment(Transform parent, Vector2 pos, RopeSettings settings, Rigidbody2D previousBody, List<Transform> segments, Line lineComponent)
        {
            // 1. Create the physical body
            Rigidbody2D currentRb = CreateSegment(parent, pos, settings, false, segments, lineComponent);

            // 2. Rotate segment to look at previous one (Visual polish)
            Vector2 dir = (previousBody.position - currentRb.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            currentRb.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // 3. Add HingeJoint to connect to previous
            HingeJoint2D joint = currentRb.gameObject.AddComponent<HingeJoint2D>();
            joint.connectedBody = previousBody;
            joint.autoConfigureConnectedAnchor = true; 
            
            // Optional: Limit angle to prevent rope acting like a stiff stick or folding entirely
            joint.useLimits = false; 

            return currentRb;
        }
    }


     [RequireComponent(typeof(Line), typeof(LineRenderer))]
    public class RopeHandler : MonoBehaviour
    {
        [SerializeField] private RopeSettings settings;
        private Line _lineComponent;
        private List<Transform> _segments;
        
        private LineRenderer _lineRenderer;
        
        private Vector3[] _positionsBuffer;
        private Vector3[] _prevPositionsBuffer;
        //[SerializeField] private Layer ropeLayer
             [Header("Performance")]
        [SerializeField] private float minMovementThreshold = 0.001f;
        private bool _isGenerated;
        private float _thresholdSqr;
        /*[SerializeField] private LayerMask ropeLayer;*/

        private void Awake(){
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
            _thresholdSqr = minMovementThreshold * minMovementThreshold;
        }

        private void OnEnable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized += OnLineFinalized;
        }

        private void OnDisable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized -= OnLineFinalized;
            CleanupSegments();
        }

        private void OnLineFinalized() => GenerateRope();

        private void GenerateRope()
        {
            if (settings == null) return;
            CleanupSegments();

            if (TryGetComponent(out Rigidbody2D parentRb)) parentRb.simulated = false; 
            if (TryGetComponent(out Collider2D parentCol)) parentCol.enabled = false;
            //Change rope layer
            //this.gameObject.layer = ropeLayer;
            // Updated Build Call passing _lineComponent
            _segments = RopeBuilder.Build(transform, _lineComponent.points, settings, _lineComponent);
            if (_segments != null && _segments.Count > 0)
            {
                int count = _segments.Count;
                _positionsBuffer = new Vector3[count];
                _prevPositionsBuffer = new Vector3[count]; // For movement delta check
                
                _lineRenderer.positionCount = count;
                _lineRenderer.useWorldSpace = true;
                _isGenerated = true;
            }
            
        }

        private void CleanupSegments()
        {
            if (_segments != null)
            {
                for (int i = 0; i < _segments.Count; i++) if (_segments[i]) Destroy(_segments[i].gameObject);
                _segments.Clear();
            }
        }
        private void LateUpdate()
        {
            if (!_isGenerated || _segments == null || _segments.Count == 0) return;
            if (!_lineRenderer.isVisible) return; // Optimization: Don't update if off-screen

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool hasMoved = false;
            int count = _segments.Count;
            
            // Safety: If array sizes mismatch (shouldn't happen), re-init
            if (_positionsBuffer.Length != count)
            {
                _positionsBuffer = new Vector3[count];
                _prevPositionsBuffer = new Vector3[count];
                _lineRenderer.positionCount = count;
            }

            // High-speed loop: avoiding Linq, heavy method calls, or excessive logic
            for (int i = 0; i < count; i++)
            {
                var seg = _segments[i];
                if (!seg) continue; // Handle case where segment was destroyed externally

                Vector3 pos = seg.position;
                _positionsBuffer[i] = pos;

                // Check movement against threshold
                if (!hasMoved)
                {
                    float sqrDist = (pos - _prevPositionsBuffer[i]).sqrMagnitude;
                    if (sqrDist > _thresholdSqr) hasMoved = true;
                }
                
                _prevPositionsBuffer[i] = pos;
            }

            // Only push data to GPU if something actually moved
            if (hasMoved)
            {
                _lineRenderer.SetPositions(_positionsBuffer);
            }
        }
    }
}

/*using System.Collections.Generic;
using UnityEngine;

namespace Drawing.LineControl
{ 
    // Lightweight component to cache parent data on segments
    public class RopeSegment : MonoBehaviour
    {
        public int RopeId { get; private set; }
        public Line ParentLine { get; private set; }

        public void Initialize(Line line)
        {
            ParentLine = line;
            // Cache the ID to avoid accessing the object later if it's destroyed
            RopeId = line.GetInstanceID(); 
        }
    }
    [System.Serializable]
    public class RopeSettings
    {
        [Min(0.01f)] public float segmentLength = 0.5f;
        [Min(0.01f)] public float segmentMass = 0.2f;
        [Min(0.01f)] public float ropeWidth = 0.2f;
        public PhysicsMaterial2D physicsMaterial;
        public bool freezeRotation = true;
        [Tooltip("If true, the root of the rope is locked in space. If false, it falls attached to the parent.")]
        public bool anchorIsStatic = true;
    }

    public static class RopeBuilder
    {
        public static List<Transform> Build(Transform parent, List<Vector2> points, RopeSettings settings, Line lineComponent)
        {
            var segments = new List<Transform>();
            
            // Basic validation
            if (parent == null || settings == null || points == null || points.Count < 2) 
                return segments;

            // 1. Create the Anchor (Root) at the first point
            Vector2 lastSpawnedPos = parent.TransformPoint(points[0]);
            Rigidbody2D previousRb = CreateSegment(parent, lastSpawnedPos, settings, true, segments, lineComponent); 
            
            if (!previousRb) return segments;

            // 2. Iterate through the raw points of the drawn line
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 targetPoint = parent.TransformPoint(points[i]);
                
                // Calculate distance from the *last physically spawned segment* to the current target point
                Vector2 directionToTarget = targetPoint - lastSpawnedPos;
                float distanceToTarget = directionToTarget.magnitude;

                // 3. The "Walking" Loop:
                // Instead of relying on the raw point spacing (which might be uneven),
                // we fill the distance with as many segments as will fit.
                // This ensures perfectly even distribution of rope segments.
                while (distanceToTarget >= settings.segmentLength)
                {
                    // Normalize direction
                    Vector2 direction = directionToTarget / distanceToTarget;
                    
                    // Calculate exact position for the new segment based on the fixed length
                    Vector2 spawnPos = lastSpawnedPos + (direction * settings.segmentLength);
                    
                    // Create and connect the segment
                    previousRb = CreateConnectedSegment(parent, spawnPos, settings, previousRb, segments, lineComponent);
                    
                    // Update tracking variables
                    lastSpawnedPos = spawnPos; // The "head" of the rope has moved forward
                    
                    // Recalculate remaining distance to the current target point
                    directionToTarget = targetPoint - lastSpawnedPos;
                    distanceToTarget = directionToTarget.magnitude;
                }
                
                // If the remaining distance is less than a segment length, we ignore it 
                // and move to the next raw point (i++), preserving the remainder for the next iteration.
            }

            return segments;
        }

        private static Rigidbody2D CreateConnectedSegment(Transform parent, Vector2 pos, RopeSettings settings, Rigidbody2D previousBody, List<Transform> segments, Line lineComponent)
        {
            Rigidbody2D rb = CreateSegment(parent, pos, settings, false, segments, lineComponent);
            
            if (rb != null && previousBody != null)
            {
                // Create the hinge joint
                var joint = rb.gameObject.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousBody;
                joint.autoConfigureConnectedAnchor = true;
                joint.useLimits = false; 

                // === CRITICAL FIX FOR STABILITY ===
                // Since we are overlapping the colliders (see CreateSegment below) to prevent tunneling,
                // we MUST ignore collisions between immediate neighbors. 
                // Otherwise, they will explode/jitter due to interpenetration.
                Collider2D col1 = rb.GetComponent<Collider2D>();
                Collider2D col2 = previousBody.GetComponent<Collider2D>();
                if (col1 && col2) Physics2D.IgnoreCollision(col1, col2);
            }
            return rb;
        }

        private static Rigidbody2D CreateSegment(Transform parent, Vector2 worldPos, RopeSettings settings, bool isAnchor, List<Transform> segments, Line lineComponent)
        {
            var segObj = new GameObject(isAnchor ? "RopeAnchor" : "RopeSegment");
            segObj.transform.position = worldPos;
            
            // Add identification component
            var segmentData = segObj.AddComponent<RopeSegment>();
            if(lineComponent != null) segmentData.Initialize(lineComponent);

            segObj.transform.SetParent(parent, true);
            segObj.layer = parent.gameObject.layer;
            segObj.tag = parent.gameObject.tag;
            
            var rb = segObj.AddComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; 
            
            // Continuous detection is expensive but necessary for ropes to prevent 
            // fast objects from passing through them.
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
            rb.linearDamping = 0.5f; 
            rb.angularDamping = 0.5f;

            if (isAnchor && settings.anchorIsStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = settings.segmentMass;
                if (settings.freezeRotation) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            var col = segObj.AddComponent<CapsuleCollider2D>();
            
            // === ANTI-TUNNELING STRATEGY ===
            // We extend the collider length slightly beyond the visual segment length.
            // This creates an physical overlap at the joints (like a ball-and-socket),
            // preventing gaps from opening up when the rope bends sharply.
            float overlap = settings.ropeWidth * 0.5f;
            col.size = new Vector2(settings.ropeWidth, settings.segmentLength + overlap);
            
            col.direction = CapsuleDirection2D.Vertical; 
            if (settings.physicsMaterial != null) col.sharedMaterial = settings.physicsMaterial;

            segments.Add(segObj.transform);
            return rb;
        }
    }

     [RequireComponent(typeof(Line), typeof(LineRenderer))]
    public class RopeHandler : MonoBehaviour
    {
        [SerializeField] private RopeSettings settings;
        private Line _lineComponent;
        private List<Transform> _segments;
        
        private LineRenderer _lineRenderer;
        
        private Vector3[] _positionsBuffer;
        private Vector3[] _prevPositionsBuffer;
        //[SerializeField] private Layer ropeLayer
             [Header("Performance")]
        [SerializeField] private float minMovementThreshold = 0.001f;
        private bool _isGenerated;
        private float _thresholdSqr;
        /*[SerializeField] private LayerMask ropeLayer;#1#

        private void Awake(){
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
            _thresholdSqr = minMovementThreshold * minMovementThreshold;
        }

        private void OnEnable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized += OnLineFinalized;
        }

        private void OnDisable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized -= OnLineFinalized;
            CleanupSegments();
        }

        private void OnLineFinalized() => GenerateRope();

        private void GenerateRope()
        {
            if (settings == null) return;
            CleanupSegments();

            if (TryGetComponent(out Rigidbody2D parentRb)) parentRb.simulated = false; 
            if (TryGetComponent(out Collider2D parentCol)) parentCol.enabled = false;
            //Change rope layer
            //this.gameObject.layer = ropeLayer;
            // Updated Build Call passing _lineComponent
            _segments = RopeBuilder.Build(transform, _lineComponent.points, settings, _lineComponent);
            if (_segments != null && _segments.Count > 0)
            {
                int count = _segments.Count;
                _positionsBuffer = new Vector3[count];
                _prevPositionsBuffer = new Vector3[count]; // For movement delta check
                
                _lineRenderer.positionCount = count;
                _lineRenderer.useWorldSpace = true;
                _isGenerated = true;
            }
            
        }

        private void CleanupSegments()
        {
            if (_segments != null)
            {
                for (int i = 0; i < _segments.Count; i++) if (_segments[i]) Destroy(_segments[i].gameObject);
                _segments.Clear();
            }
        }
        private void LateUpdate()
        {
            if (!_isGenerated || _segments == null || _segments.Count == 0) return;
            if (!_lineRenderer.isVisible) return; // Optimization: Don't update if off-screen

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool hasMoved = false;
            int count = _segments.Count;
            
            // Safety: If array sizes mismatch (shouldn't happen), re-init
            if (_positionsBuffer.Length != count)
            {
                _positionsBuffer = new Vector3[count];
                _prevPositionsBuffer = new Vector3[count];
                _lineRenderer.positionCount = count;
            }

            // High-speed loop: avoiding Linq, heavy method calls, or excessive logic
            for (int i = 0; i < count; i++)
            {
                var seg = _segments[i];
                if (!seg) continue; // Handle case where segment was destroyed externally

                Vector3 pos = seg.position;
                _positionsBuffer[i] = pos;

                // Check movement against threshold
                if (!hasMoved)
                {
                    float sqrDist = (pos - _prevPositionsBuffer[i]).sqrMagnitude;
                    if (sqrDist > _thresholdSqr) hasMoved = true;
                }
                
                _prevPositionsBuffer[i] = pos;
            }

            // Only push data to GPU if something actually moved
            if (hasMoved)
            {
                _lineRenderer.SetPositions(_positionsBuffer);
            }
        }
    }
}*/
/*using System.Collections.Generic;
using UnityEngine;
namespace Drawing.LineControl
{
    [System.Serializable]
    public class RopeSettings
    {
        [Min(0.01f)] public float segmentLength = 0.5f;
        [Min(0.01f)] public float segmentMass = 0.2f;
        [Min(0.01f)] public float ropeWidth = 0.2f;
        public PhysicsMaterial2D physicsMaterial;
        public bool freezeRotation = true;
        [Tooltip("If true, the root of the rope is locked in space. If false, it falls attached to the parent.")]
        public bool anchorIsStatic = true;
    }
 
        // Lightweight component to cache parent data on segments
        public class RopeSegment : MonoBehaviour
        {
            public int RopeId { get; private set; }
            public Line ParentLine { get; private set; }

            public void Initialize(Line line)
            {
                ParentLine = line;
                // Cache the ID to avoid accessing the object later if it's destroyed
                RopeId = line.GetInstanceID(); 
            }
        }
    

public static class RopeBuilder
    {
        // Added Line reference to signature to pass it to segments
        public static List<Transform> Build(Transform parent, List<Vector2> points, RopeSettings settings, Line lineComponent)
        {
            var segments = new List<Transform>();
            if (parent == null || settings == null || points == null || points.Count < 2) 
                return segments;

            Vector2 startPos = parent.TransformPoint(points[0]);
            // Pass lineComponent
            Rigidbody2D previousRb = CreateSegment(parent, startPos, settings, true, segments, lineComponent); 
            
            if (!previousRb) return segments;

            float distanceAccumulator = 0f;
            Vector2 lastWorldPos = startPos;

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentWorldPos = parent.TransformPoint(points[i]);
                float dist = Vector2.Distance(currentWorldPos, lastWorldPos);
                distanceAccumulator += dist;

                if (distanceAccumulator >= settings.segmentLength)
                {
                    Vector2 direction = (currentWorldPos - lastWorldPos).normalized;
                    Vector2 spawnPos = lastWorldPos + (direction * (dist - (distanceAccumulator - settings.segmentLength)));
                    previousRb = CreateConnectedSegment(parent, currentWorldPos, settings, previousRb, segments, lineComponent);
                    lastWorldPos = currentWorldPos;
                    distanceAccumulator = 0f;
                }
                else
                {
                    lastWorldPos = currentWorldPos;
                }
            }

            if (distanceAccumulator > settings.segmentLength * 0.5f)
            {
                Vector2 endPos = parent.TransformPoint(points[points.Count - 1]);
                CreateConnectedSegment(parent, endPos, settings, previousRb, segments, lineComponent);
            }
            return segments;
        }

        private static Rigidbody2D CreateConnectedSegment(Transform parent, Vector2 pos, RopeSettings settings, Rigidbody2D previousBody, List<Transform> segments, Line lineComponent)
        {
            Rigidbody2D rb = CreateSegment(parent, pos, settings, false, segments, lineComponent);
            if (rb != null && previousBody != null)
            {
                var joint = rb.gameObject.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousBody;
                joint.autoConfigureConnectedAnchor = true;
                joint.useLimits = false; 
            }
            return rb;
        }

        private static Rigidbody2D CreateSegment(Transform parent, Vector2 worldPos, RopeSettings settings, bool isAnchor, List<Transform> segments, Line lineComponent)
        {
            var segObj = new GameObject(isAnchor ? "RopeAnchor" : "RopeSegment");
            segObj.transform.position = worldPos;
            
            // Optimization: Add identifier component immediately
            var segmentData = segObj.AddComponent<RopeSegment>();
            if(lineComponent != null) segmentData.Initialize(lineComponent);

            segObj.transform.SetParent(parent, true);
            segObj.layer = parent.gameObject.layer;
            segObj.tag = parent.gameObject.tag;
            
            var rb = segObj.AddComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; 
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0.5f; 
            rb.angularDamping = 0.5f;

            if (isAnchor && settings.anchorIsStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = settings.segmentMass;
                if (settings.freezeRotation) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            var col = segObj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(settings.ropeWidth, settings.segmentLength);
            col.direction = CapsuleDirection2D.Vertical; 
            if (settings.physicsMaterial != null) col.sharedMaterial = settings.physicsMaterial;

            segments.Add(segObj.transform);
            return rb;
        }
    }

    [RequireComponent(typeof(Line), typeof(LineRenderer))]
    public class RopeHandler : MonoBehaviour
    {
        [SerializeField] private RopeSettings settings;
        private Line _lineComponent;
        private List<Transform> _segments;
        
        private LineRenderer _lineRenderer;
        
        private Vector3[] _positionsBuffer;
        private Vector3[] _prevPositionsBuffer;
        [Header("Performance")]
        [SerializeField] private float minMovementThreshold = 0.001f;
        private bool _isGenerated;
        private float _thresholdSqr;

        private void Awake(){
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
            _thresholdSqr = minMovementThreshold * minMovementThreshold;
        }

        private void OnEnable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized += OnLineFinalized;
        }

        private void OnDisable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized -= OnLineFinalized;
            CleanupSegments();
        }

        private void OnLineFinalized() => GenerateRope();

        private void GenerateRope()
        {
            if (settings == null) return;
            CleanupSegments();

            if (TryGetComponent(out Rigidbody2D parentRb)) parentRb.simulated = false; 
            if (TryGetComponent(out Collider2D parentCol)) parentCol.enabled = false;

            // Updated Build Call passing _lineComponent
            _segments = RopeBuilder.Build(transform, _lineComponent.points, settings, _lineComponent);
            if (_segments != null && _segments.Count > 0)
            {
                int count = _segments.Count;
                _positionsBuffer = new Vector3[count];
                _prevPositionsBuffer = new Vector3[count]; // For movement delta check
                
                _lineRenderer.positionCount = count;
                _lineRenderer.useWorldSpace = true;
                _isGenerated = true;
            }
            
        }

        private void CleanupSegments()
        {
            if (_segments != null)
            {
                for (int i = 0; i < _segments.Count; i++) if (_segments[i]) Destroy(_segments[i].gameObject);
                _segments.Clear();
            }
        }
        private void LateUpdate()
        {
            if (!_isGenerated || _segments == null || _segments.Count == 0) return;
            if (!_lineRenderer.isVisible) return; // Optimization: Don't update if off-screen

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool hasMoved = false;
            int count = _segments.Count;
            
            // Safety: If array sizes mismatch (shouldn't happen), re-init
            if (_positionsBuffer.Length != count)
            {
                _positionsBuffer = new Vector3[count];
                _prevPositionsBuffer = new Vector3[count];
                _lineRenderer.positionCount = count;
            }

            // High-speed loop: avoiding Linq, heavy method calls, or excessive logic
            for (int i = 0; i < count; i++)
            {
                var seg = _segments[i];
                if (!seg) continue; // Handle case where segment was destroyed externally

                Vector3 pos = seg.position;
                _positionsBuffer[i] = pos;

                // Check movement against threshold
                if (!hasMoved)
                {
                    float sqrDist = (pos - _prevPositionsBuffer[i]).sqrMagnitude;
                    if (sqrDist > _thresholdSqr) hasMoved = true;
                }
                
                _prevPositionsBuffer[i] = pos;
            }

            // Only push data to GPU if something actually moved
            if (hasMoved)
            {
                _lineRenderer.SetPositions(_positionsBuffer);
            }
        }
    }
}*/



/*namespace Drawing.LineControl
{
    [System.Serializable]
    public class RopeSettings
    {
        [Min(0.01f)] public float segmentLength = 0.5f;
        [Min(0.01f)] public float segmentMass = 0.2f;
        [Min(0.01f)] public float ropeWidth = 0.2f;
        public PhysicsMaterial2D physicsMaterial;
        [Tooltip("If true, segments will remain upright and won't spin.")]
        public bool freezeRotation = true;
        [Tooltip("If true, the anchor (first segment) will be static. If false, it will be dynamic.")]
        public bool anchorIsStatic = true;
    }

    public static class RopeBuilder
    {
        public static List<Transform> Build(Transform parent, List<Vector2> points, RopeSettings settings, bool enableDebugLogs = false)
        {
            // Safety checks
            if (parent == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Parent Transform is null. Cannot build rope.");
                return new List<Transform>();
            }

            if (settings == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] RopeSettings is null. Cannot build rope.");
                return new List<Transform>();
            }

            StripPhysicsComponents(parent);

            var segments = new List<Transform>();
            if (points == null || points.Count < 2)
            {
                if (enableDebugLogs) Debug.LogWarning($"[RopeBuilder] Invalid points: {(points == null ? "null" : $"count={points.Count}")}. Need at least 2 points.");
                return segments;
            }

            Matrix4x4 localToWorld = parent.localToWorldMatrix;

            if (enableDebugLogs) Debug.Log($"[RopeBuilder] Building rope with {points.Count} points, segmentLength={settings.segmentLength}");

            // 1. Create Anchor
            Vector2 startPos = localToWorld.MultiplyPoint3x4(points[0]);
            Rigidbody2D previousBody = CreateAnchor(parent, startPos, settings, segments, enableDebugLogs);

            // 2. Build Chain
            float distanceAccumulator = 0f;
            Vector2 lastSegmentPos = startPos;

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentPointWorld = localToWorld.MultiplyPoint3x4(points[i]);
                float distSqr = (currentPointWorld - lastSegmentPos).sqrMagnitude;
                distanceAccumulator += Mathf.Sqrt(distSqr);

                if (distanceAccumulator >= settings.segmentLength)
                {
                    previousBody = AddConnectedSegment(parent, currentPointWorld, settings, previousBody, segments, enableDebugLogs);
                    lastSegmentPos = currentPointWorld;
                    distanceAccumulator = 0f;
                }
            }

            // 3. Create Final Segment (if needed)
            if (distanceAccumulator > 0.01f)
            {
                Vector2 endPos = localToWorld.MultiplyPoint3x4(points[points.Count - 1]);
                AddConnectedSegment(parent, endPos, settings, previousBody, segments, enableDebugLogs);
            }

            if (enableDebugLogs) Debug.Log($"[RopeBuilder] Rope built successfully with {segments.Count} segments.");
            return segments;
        }

        private static void StripPhysicsComponents(Transform target)
        {
            if (target.TryGetComponent(out PolygonCollider2D poly)) Object.Destroy(poly);
            if (target.TryGetComponent(out EdgeCollider2D edge)) Object.Destroy(edge);
            if (target.TryGetComponent(out Rigidbody2D rb)) Object.Destroy(rb);
        }

        private static Rigidbody2D CreateAnchor(Transform parent, Vector2 pos, RopeSettings settings, List<Transform> segments, bool enableDebugLogs = false)
        {
            GameObject anchor = CreateBaseSegment(parent, pos, settings, settings.anchorIsStatic, enableDebugLogs);
            if (anchor == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Failed to create anchor segment.");
                return null;
            }

            Rigidbody2D rb = anchor.GetComponent<Rigidbody2D>();
            if (rb == null && enableDebugLogs)
            {
                Debug.LogError("[RopeBuilder] Anchor segment missing Rigidbody2D component.");
            }

            segments.Add(anchor.transform);
            if (enableDebugLogs) Debug.Log($"[RopeBuilder] Created anchor at position {pos}");
            return rb;
        }

        private static Rigidbody2D AddConnectedSegment(Transform parent, Vector2 pos, RopeSettings settings, Rigidbody2D previousBody, List<Transform> segments, bool enableDebugLogs = false)
        {
            // Safety check
            if (previousBody == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Cannot add connected segment: previousBody is null.");
                return null;
            }

            GameObject seg = CreateBaseSegment(parent, pos, settings, false, enableDebugLogs);
            if (seg == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Failed to create connected segment.");
                return null;
            }

            // Add Joint
            var joint = seg.AddComponent<HingeJoint2D>();
            if (joint == null && enableDebugLogs)
            {
                Debug.LogError("[RopeBuilder] Failed to add HingeJoint2D to segment.");
            }
            else
            {
                joint.connectedBody = previousBody;
                joint.autoConfigureConnectedAnchor = true;
            }

            segments.Add(seg.transform);
            Rigidbody2D rb = seg.GetComponent<Rigidbody2D>();
            if (rb == null && enableDebugLogs)
            {
                Debug.LogError("[RopeBuilder] Connected segment missing Rigidbody2D component.");
            }

            return rb;
        }

        private static GameObject CreateBaseSegment(Transform parent, Vector2 worldPos, RopeSettings settings, bool isStatic, bool enableDebugLogs = false)
        {
            // Safety checks
            if (parent == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Cannot create segment: parent is null.");
                return null;
            }

            if (settings == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Cannot create segment: settings is null.");
                return null;
            }

            var seg = new GameObject("RopeSegment");
            if (seg == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Failed to create GameObject for segment.");
                return null;
            }

            seg.transform.position = worldPos;
            seg.transform.SetParent(parent, true);
            seg.layer = parent.gameObject.layer;
            seg.tag = parent.gameObject.tag;

            var rb = seg.AddComponent<Rigidbody2D>();
            if (rb == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Failed to add Rigidbody2D to segment.");
                Object.Destroy(seg);
                return null;
            }

            if (settings.freezeRotation)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = settings.segmentMass;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            var col = seg.AddComponent<CapsuleCollider2D>();
            if (col == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeBuilder] Failed to add CapsuleCollider2D to segment.");
            }
            else
            {
                col.size = new Vector2(settings.ropeWidth, settings.segmentLength);
                if (settings.physicsMaterial != null) col.sharedMaterial = settings.physicsMaterial;
            }

            return seg;
        }
    }

    [RequireComponent(typeof(Line), typeof(LineRenderer))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RopeSettings settings;

        [Header("Performance")]
        [Tooltip("0 = update every frame, 1 = every other frame")]
        [SerializeField] private int updateSkipFrames = 0;
        [SerializeField] private float minMovementThreshold = 0.001f;

        [Header("Debug")]
        [Tooltip("Enable debug messages in the console")]
        [SerializeField] private bool enableDebugLogs = false;

        private Line _lineComponent;
        private LineRenderer _lineRenderer;

        private List<Transform> _segments = new List<Transform>();
        private Vector3[] _positionsBuffer;
        private Vector3[] _previousPositionsBuffer;

        private bool _isGenerated;
        private int _frameCounter;
        private bool _isVisible = true;

        private void Awake()
        {
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();

            // Safety checks
            if (_lineComponent == null)
            {
                Debug.LogError("[RopeHandler] Line component is missing! RopeHandler requires a Line component.");
            }

            if (_lineRenderer == null)
            {
                Debug.LogError("[RopeHandler] LineRenderer component is missing! RopeHandler requires a LineRenderer component.");
            }

            if (enableDebugLogs && _lineComponent != null && _lineRenderer != null)
            {
                Debug.Log("[RopeHandler] Initialized successfully.");
            }
        }

        private void OnEnable()
        {
            _isVisible = true;
            if (_lineComponent != null) _lineComponent.OnLineFinalized += OnLineFinalized;
        }

        private void OnDisable()
        {
            if (_lineComponent != null) _lineComponent.OnLineFinalized -= OnLineFinalized;
            CleanupSegments();
        }

        private void OnLineFinalized() => GenerateRope();

        private void GenerateRope()
        {
            // Safety checks
            if (_lineComponent == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeHandler] Cannot generate rope: Line component is null.");
                return;
            }

            if (settings == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeHandler] Cannot generate rope: RopeSettings is null.");
                return;
            }

            if (enableDebugLogs) Debug.Log("[RopeHandler] Starting rope generation...");

            CleanupSegments();

            List<Vector2> points = _lineComponent.points;
            if (points == null || points.Count < 2)
            {
                if (enableDebugLogs) Debug.LogWarning($"[RopeHandler] Invalid points for rope generation: {(points == null ? "null" : $"count={points.Count}")}.");
                return;
            }

            _segments = RopeBuilder.Build(transform, points, settings, enableDebugLogs);

            if (_segments == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeHandler] RopeBuilder.Build returned null segments list.");
                _isGenerated = false;
                return;
            }

            if (_segments.Count > 0)
            {
                InitializeBuffers(_segments.Count);
                _isGenerated = true;
                if (enableDebugLogs) Debug.Log($"[RopeHandler] Rope generated successfully with {_segments.Count} segments.");
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning("[RopeHandler] Rope generation completed but no segments were created.");
                _isGenerated = false;
            }
        }

        private void LateUpdate()
        {
            if (!CanUpdateVisuals()) return;

            if (ShouldSkipUpdate()) return;

            UpdateVisuals();
        }

        private bool CanUpdateVisuals()
        {
            return _isGenerated && _segments.Count > 0 && _isVisible;
        }

        private bool ShouldSkipUpdate()
        {
            if (updateSkipFrames <= 0) return false;

            _frameCounter++;
            if (_frameCounter <= updateSkipFrames) return true;

            _frameCounter = 0;
            return false;
        }

        private void UpdateVisuals()
        {
            // Safety checks
            if (_lineRenderer == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeHandler] Cannot update visuals: LineRenderer is null.");
                return;
            }

            if (_segments == null || _segments.Count == 0)
            {
                if (enableDebugLogs) Debug.LogWarning("[RopeHandler] Cannot update visuals: segments list is null or empty.");
                return;
            }

            EnsureBuffersSize(_segments.Count);

            if (ProcessSegmentMovements(out float thresholdSqr))
            {
                if (_positionsBuffer == null)
                {
                    if (enableDebugLogs) Debug.LogError("[RopeHandler] Cannot update visuals: positions buffer is null.");
                    return;
                }

                _lineRenderer.SetPositions(_positionsBuffer);
            }
        }

        private void InitializeBuffers(int count)
        {
            // Safety checks
            if (count <= 0)
            {
                if (enableDebugLogs) Debug.LogWarning($"[RopeHandler] Cannot initialize buffers: invalid count={count}.");
                return;
            }

            if (_lineRenderer == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeHandler] Cannot initialize buffers: LineRenderer is null.");
                return;
            }

            if (_segments == null || _segments.Count != count)
            {
                if (enableDebugLogs) Debug.LogWarning($"[RopeHandler] Segments count mismatch: expected {count}, actual {(_segments?.Count ?? 0)}.");
            }

            _positionsBuffer = new Vector3[count];
            _previousPositionsBuffer = new Vector3[count];

            _lineRenderer.positionCount = count;
            _lineRenderer.useWorldSpace = true;

            for (int i = 0; i < count; i++)
            {
                if (_segments != null && i < _segments.Count && _segments[i] != null)
                {
                    Vector3 pos = _segments[i].position;
                    _previousPositionsBuffer[i] = pos;
                    _positionsBuffer[i] = pos;
                }
                else
                {
                    // Initialize with zero if segment is missing
                    _previousPositionsBuffer[i] = Vector3.zero;
                    _positionsBuffer[i] = Vector3.zero;
                    if (enableDebugLogs && i == 0) Debug.LogWarning($"[RopeHandler] Segment at index {i} is null during buffer initialization.");
                }
            }

            _lineRenderer.SetPositions(_positionsBuffer);
            if (enableDebugLogs) Debug.Log($"[RopeHandler] Initialized buffers for {count} segments.");
        }

        private void EnsureBuffersSize(int count)
        {
            if (_positionsBuffer == null || _positionsBuffer.Length != count)
            {
                InitializeBuffers(count);
            }
        }

        /// <summary>
        /// Iterates over segments, updates the buffer, and checks if enough movement occurred to justify a redraw.
        /// </summary>
        private bool ProcessSegmentMovements(out float thresholdSqr)
        {
            thresholdSqr = minMovementThreshold * minMovementThreshold;
            bool hasSignificantMovement = false;

            // Safety checks
            if (_segments == null || _segments.Count == 0)
            {
                if (enableDebugLogs) Debug.LogWarning("[RopeHandler] Cannot process movements: segments list is null or empty.");
                return false;
            }

            if (_positionsBuffer == null || _previousPositionsBuffer == null)
            {
                if (enableDebugLogs) Debug.LogError("[RopeHandler] Cannot process movements: buffers are null.");
                return false;
            }

            int count = _segments.Count;
            int nullSegmentCount = 0;

            for (int i = 0; i < count; i++)
            {
                if (_segments[i] == null)
                {
                    nullSegmentCount++;
                    if (enableDebugLogs && nullSegmentCount == 1) Debug.LogWarning($"[RopeHandler] Found null segment at index {i} during movement processing.");
                    continue;
                }

                if (i >= _positionsBuffer.Length || i >= _previousPositionsBuffer.Length)
                {
                    if (enableDebugLogs) Debug.LogError($"[RopeHandler] Buffer index out of range: {i} (buffer length: {_positionsBuffer.Length}).");
                    continue;
                }

                Vector3 currentPos = _segments[i].position;
                _positionsBuffer[i] = currentPos;

                if (!hasSignificantMovement)
                {
                    float moveSqr = (currentPos - _previousPositionsBuffer[i]).sqrMagnitude;
                    if (moveSqr > thresholdSqr)
                    {
                        hasSignificantMovement = true;
                    }
                }

                _previousPositionsBuffer[i] = currentPos;
            }

            if (enableDebugLogs && nullSegmentCount > 0)
            {
                Debug.LogWarning($"[RopeHandler] Processed movements: {nullSegmentCount} null segments found out of {count} total.");
            }

            return hasSignificantMovement;
        }

        private void CleanupSegments()
        {
            _isGenerated = false;

            if (_segments == null)
            {
                if (enableDebugLogs) Debug.LogWarning("[RopeHandler] CleanupSegments called but segments list is null.");
                return;
            }

            int destroyedCount = 0;
            foreach (var segment in _segments)
            {
                if (segment != null)
                {
                    Destroy(segment.gameObject);
                    destroyedCount++;
                }
            }

            _segments.Clear();

            if (enableDebugLogs) Debug.Log($"[RopeHandler] Cleaned up {destroyedCount} segments.");
        }

        public void SetVisible(bool state)
        {
            _isVisible = state;

            if (_lineRenderer == null)
            {
                if (enableDebugLogs) Debug.LogWarning("[RopeHandler] Cannot set visibility: LineRenderer is null.");
                return;
            }

            _lineRenderer.enabled = state;
            if (enableDebugLogs) Debug.Log($"[RopeHandler] Visibility set to: {state}");
        }
    }
}*/











//___________________________________________
/*using UnityEngine;
using System.Collections.Generic;
using Drawing.LineControl;

namespace Drawing.Mechanics
{
    [RequireComponent(typeof(Line))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Rope Settings")]
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float segmentMass = 0.2f;
        [SerializeField] private float ropeWidth = 0.2f;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D ropePhysicsMaterial;

        // References
        private Line _lineComponent;
        private LineRenderer _lineRenderer;

        private List<Transform> _segments = new List<Transform>();
        private List<LineRenderer> _segmentLineRenderers = new List<LineRenderer>(); // Cache LineRenderers

        // Optimization: Cache the positions array to avoid GC allocation every frame
        private Vector3[] _positionsBuffer;
        private bool _isGenerated = false;
        private bool _isVisible = true;

        private void Awake()
        {
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized += GenerateRopePhysics;
            }
        }

        private void OnDestroy()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized -= GenerateRopePhysics;
            }
        }

        // Optimization: Only update visual line when visible by any camera
        private void OnBecameVisible() => _isVisible = true;
        private void OnBecameInvisible() => _isVisible = false;

        private void GenerateRopePhysics()
        {
            // 1. Disable the main visual line (since segments will have their own lines)
            if (_lineRenderer != null) _lineRenderer.enabled = false;

            // 2. Destroy original physics
            if (TryGetComponent(out PolygonCollider2D poly)) Destroy(poly);
            if (TryGetComponent(out EdgeCollider2D edge)) Destroy(edge);
            if (TryGetComponent(out Rigidbody2D rb)) Destroy(rb);

            // 3. Validate points
            List<Vector2> points = _lineComponent.points;
            if (points == null || points.Count < 2) return;

            // 4. Build Chain
            CreateChain(points);

            // 5. Initialize buffer for main line (if we want to keep it as backup)
            _positionsBuffer = new Vector3[_segments.Count];
            _lineRenderer.positionCount = _segments.Count;
            _lineRenderer.useWorldSpace = true;

            _isGenerated = true;
        }

        private void CreateChain(List<Vector2> points)
        {
            Rigidbody2D previousRB = null;
            Transform previousTransform = null;

            // Anchor
            Vector2 startPos = transform.TransformPoint(points[0]);
            GameObject anchor = CreateSegment(startPos, true);
            previousRB = anchor.GetComponent<Rigidbody2D>();
            previousTransform = anchor.transform;
            _segments.Add(previousTransform);

            float distanceAccumulator = 0f;
            Vector2 lastSegmentPos = startPos;

            // Segments
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentPointWorld = transform.TransformPoint(points[i]);
                float dist = Vector2.Distance(lastSegmentPos, currentPointWorld);
                distanceAccumulator += dist;

                if (distanceAccumulator >= segmentLength)
                {
                    GameObject newSeg = CreateSegment(currentPointWorld, false);
                    Transform newTransform = newSeg.transform;
                    Rigidbody2D newRB = newSeg.GetComponent<Rigidbody2D>();

                    // Create LineRenderer for previous segment that connects to this one
                    if (previousTransform != null)
                    {
                        CreateSegmentLineRenderer(previousTransform, newTransform);
                    }

                    HingeJoint2D joint = newSeg.AddComponent<HingeJoint2D>();
                    joint.connectedBody = previousRB;
                    joint.autoConfigureConnectedAnchor = true;

                    previousRB = newRB;
                    previousTransform = newTransform;
                    _segments.Add(newTransform);

                    lastSegmentPos = currentPointWorld;
                    distanceAccumulator = 0f;
                }
            }

            // End Segment
            if (distanceAccumulator > 0.01f)
            {
                Vector2 endPos = transform.TransformPoint(points[points.Count - 1]);
                GameObject endSeg = CreateSegment(endPos, false);
                Transform endTransform = endSeg.transform;

                // Create LineRenderer for previous segment that connects to end
                if (previousTransform != null)
                {
                    CreateSegmentLineRenderer(previousTransform, endTransform);
                }

                HingeJoint2D joint = endSeg.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousRB;
                joint.autoConfigureConnectedAnchor = true;
                _segments.Add(endTransform);
            }
        }

        private GameObject CreateSegment(Vector2 worldPos, bool isStatic)
        {
            GameObject seg = new GameObject("RopeSegment");
            seg.transform.position = worldPos;
            seg.transform.parent = transform;
            seg.layer = gameObject.layer;
            seg.tag = gameObject.tag;

            Rigidbody2D rb = seg.AddComponent<Rigidbody2D>();
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = segmentMass;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            CapsuleCollider2D col = seg.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(ropeWidth, segmentLength);
            if (ropePhysicsMaterial != null) col.sharedMaterial = ropePhysicsMaterial;

            return seg;
        }

        /// <summary>
        /// Creates a LineRenderer on the fromSegment that connects to the toSegment.
        /// Copies all settings from the main LineRenderer.
        /// </summary>
        private void CreateSegmentLineRenderer(Transform fromSegment, Transform toSegment)
        {
            LineRenderer segLine = fromSegment.gameObject.AddComponent<LineRenderer>();

            // Copy ALL settings from main LineRenderer
            if (_lineRenderer != null)
            {
                // Material and appearance
                segLine.material = _lineRenderer.material;
                segLine.colorGradient = _lineRenderer.colorGradient;
                segLine.startColor = _lineRenderer.startColor;
                segLine.endColor = _lineRenderer.endColor;

                // Width settings
                segLine.startWidth = _lineRenderer.startWidth;
                segLine.endWidth = _lineRenderer.endWidth;
                segLine.widthMultiplier = _lineRenderer.widthMultiplier;
                segLine.widthCurve = _lineRenderer.widthCurve;

                // Texture and rendering
                segLine.textureMode = _lineRenderer.textureMode;
                segLine.alignment = _lineRenderer.alignment;
                segLine.shadowCastingMode = _lineRenderer.shadowCastingMode;
                segLine.receiveShadows = _lineRenderer.receiveShadows;
                segLine.lightProbeUsage = _lineRenderer.lightProbeUsage;
                segLine.reflectionProbeUsage = _lineRenderer.reflectionProbeUsage;

                // Vertex settings
                segLine.numCapVertices = _lineRenderer.numCapVertices;
                segLine.numCornerVertices = _lineRenderer.numCornerVertices;

                // Sorting
                segLine.sortingLayerName = _lineRenderer.sortingLayerName;
                segLine.sortingOrder = _lineRenderer.sortingOrder;
            }
            else
            {
                // Fallback if main renderer is missing
                segLine.startWidth = ropeWidth;
                segLine.endWidth = ropeWidth;
            }

            // Configure for world space (so it connects world positions)
            segLine.useWorldSpace = true;
            segLine.positionCount = 2;

            // Store reference for updates
            _segmentLineRenderers.Add(segLine);
        }

        private void LateUpdate()
        {
            // Optimization:
            // 1. Check if generated
            // 2. Check if segments exist (safety)
            // 3. Check if visible (Culling)
            if (!_isGenerated || _segments.Count == 0 || !_isVisible) return;

            // Safety check if first segment destroyed
            if (_segments[0] == null)
            {
                _isGenerated = false;
                return;
            }

            // Update each segment's LineRenderer to connect to the next segment
            for (int i = 0; i < _segments.Count - 1; i++)
            {
                if (_segments[i] == null || _segments[i + 1] == null) continue;

                // Find the LineRenderer for this segment
                if (i < _segmentLineRenderers.Count && _segmentLineRenderers[i] != null)
                {
                    LineRenderer segLine = _segmentLineRenderers[i];

                    // Update positions to connect current segment to next segment
                    segLine.SetPosition(0, _segments[i].position);
                    segLine.SetPosition(1, _segments[i + 1].position);
                }
            }

            // Also update main LineRenderer as backup (optional - can be disabled)
            // Batch Update: Fill buffer then set all positions at once
            int count = _segments.Count;
            for (int i = 0; i < count; i++)
            {
                if (_segments[i] != null)
                {
                    _positionsBuffer[i] = _segments[i].position;
                }
            }
            _lineRenderer.SetPositions(_positionsBuffer);
        }
    }
}*/

//___________________________________________________________
/*using UnityEngine;
using System.Collections.Generic;
using Drawing.LineControl;

namespace Drawing.Mechanics
{
    [RequireComponent(typeof(Line))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Rope Settings")]
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float segmentMass = 0.2f;
        [SerializeField] private float ropeWidth = 0.2f;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D ropePhysicsMaterial;

        // References
        private Line _lineComponent;
        private LineRenderer _lineRenderer;
        
        private List<Transform> _segments = new List<Transform>();
        
        // Optimization: Cache the positions array to avoid GC allocation every frame
        private Vector3[] _positionsBuffer; 
        private bool _isGenerated = false;
        private bool _isVisible = true;

        private void Awake()
        {
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized += GenerateRopePhysics;
            }
        }

        private void OnDestroy()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized -= GenerateRopePhysics;
            }
        }

        // Optimization: Only update visual line when visible by any camera
        private void OnBecameVisible() => _isVisible = true;
        private void OnBecameInvisible() => _isVisible = false;

        private void GenerateRopePhysics()
        {
            // 1. Destroy original physics
            if (TryGetComponent(out PolygonCollider2D poly)) Destroy(poly);
            if (TryGetComponent(out EdgeCollider2D edge)) Destroy(edge);
            if (TryGetComponent(out Rigidbody2D rb)) Destroy(rb);

            // 2. Validate points
            List<Vector2> points = _lineComponent.points;
            if (points == null || points.Count < 2) return;

            // 3. Build Chain
            CreateChain(points);
            
            // 4. Optimization: Initialize the buffer array once
            _positionsBuffer = new Vector3[_segments.Count];
            _lineRenderer.positionCount = _segments.Count;
            _lineRenderer.useWorldSpace = true;

            _isGenerated = true;
        }

        private void CreateChain(List<Vector2> points)
        {
            Rigidbody2D previousRB = null;

            // Anchor
            Vector2 startPos = transform.TransformPoint(points[0]);
            GameObject anchor = CreateSegment(startPos, true);
            previousRB = anchor.GetComponent<Rigidbody2D>();
            _segments.Add(anchor.transform);

            float distanceAccumulator = 0f;
            Vector2 lastSegmentPos = startPos;

            // Segments
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentPointWorld = transform.TransformPoint(points[i]);
                float dist = Vector2.Distance(lastSegmentPos, currentPointWorld);
                distanceAccumulator += dist;

                if (distanceAccumulator >= segmentLength)
                {
                    GameObject newSeg = CreateSegment(currentPointWorld, false);
                    HingeJoint2D joint = newSeg.AddComponent<HingeJoint2D>();
                    joint.connectedBody = previousRB;
                    joint.autoConfigureConnectedAnchor = true;

                    previousRB = newSeg.GetComponent<Rigidbody2D>();
                    _segments.Add(newSeg.transform);
                    
                    lastSegmentPos = currentPointWorld;
                    distanceAccumulator = 0f;
                }
            }

            // End Segment
            if (distanceAccumulator > 0.01f)
            {
                Vector2 endPos = transform.TransformPoint(points[points.Count - 1]);
                GameObject endSeg = CreateSegment(endPos, false);
                HingeJoint2D joint = endSeg.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousRB;
                joint.autoConfigureConnectedAnchor = true;
                _segments.Add(endSeg.transform);
            }
        }

        private GameObject CreateSegment(Vector2 worldPos, bool isStatic)
        {
            GameObject seg = new GameObject("RopeSegment");
            seg.transform.position = worldPos;
            seg.transform.parent = transform;
            seg.layer = gameObject.layer; 
            seg.tag = gameObject.tag;

            Rigidbody2D rb = seg.AddComponent<Rigidbody2D>();
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = segmentMass;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            CapsuleCollider2D col = seg.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(ropeWidth, segmentLength);
            if (ropePhysicsMaterial != null) col.sharedMaterial = ropePhysicsMaterial;

            return seg;
        }

        private void LateUpdate()
        {
            // Optimization:
            // 1. Check if generated
            // 2. Check if segments exist (safety)
            // 3. Check if visible (Culling)
            if (!_isGenerated || _segments.Count == 0 || !_isVisible) return;

            // Safety check if first segment destroyed
            if (_segments[0] == null)
            {
                _isGenerated = false;
                return;
            }

            // Batch Update: Fill buffer then set all positions at once
            int count = _segments.Count;
            for (int i = 0; i < count; i++)
            {
                _positionsBuffer[i] = _segments[i].position;
            }

            _lineRenderer.SetPositions(_positionsBuffer);
        }
    }
}*/
//___________________________________________________________

/*
using UnityEngine;
using System.Collections.Generic;
/*using Drawing.LineControl;

namespace Drawing.Mechanics
{
    [RequireComponent(typeof(Line))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Rope Settings")]
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float segmentMass = 0.2f;
        [SerializeField] private float ropeWidth = 0.2f;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D ropePhysicsMaterial;

        // References
        private Line _lineComponent;
        private LineRenderer _mainLineRenderer; // The original renderer (we will hide it)
        
        // No need to store segments list for Update anymore!
        
        private void Awake()
        {
            _lineComponent = GetComponent<Line>();
            _mainLineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized += GenerateRopePhysics;
            }
        }

        private void OnDestroy()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized -= GenerateRopePhysics;
            }
        }

        private void GenerateRopePhysics()
        {
            // 1. Disable the main visual line (since segments will have their own lines)
            if (_mainLineRenderer != null) _mainLineRenderer.enabled = false;

            // 2. Destroy original physics
            if (TryGetComponent(out PolygonCollider2D poly)) Destroy(poly);
            if (TryGetComponent(out EdgeCollider2D edge)) Destroy(edge);
            if (TryGetComponent(out Rigidbody2D rb)) Destroy(rb);

            // 3. Validate points
            List<Vector2> points = _lineComponent.points;
            if (points == null || points.Count < 2) return;

            // 4. Build Chain
            CreateChain(points);
        }

        private void CreateChain(List<Vector2> points)
        {
            Rigidbody2D previousRB = null;

            // Create Anchor (Static start)
            Vector2 startPos = transform.TransformPoint(points[0]);
            GameObject anchor = CreateSegment(startPos, true);
            previousRB = anchor.GetComponent<Rigidbody2D>();

            float distanceAccumulator = 0f;
            Vector2 lastSegmentPos = startPos;

            // Create Segments
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentPointWorld = transform.TransformPoint(points[i]);
                float dist = Vector2.Distance(lastSegmentPos, currentPointWorld);
                distanceAccumulator += dist;

                if (distanceAccumulator >= segmentLength)
                {
                    GameObject newSeg = CreateSegment(currentPointWorld, false);
                    
                    // Connect Physics
                    HingeJoint2D joint = newSeg.AddComponent<HingeJoint2D>();
                    joint.connectedBody = previousRB;
                    joint.autoConfigureConnectedAnchor = true;

                    previousRB = newSeg.GetComponent<Rigidbody2D>();
                    
                    lastSegmentPos = currentPointWorld;
                    distanceAccumulator = 0f;
                }
            }

            // End Segment
            if (distanceAccumulator > 0.01f)
            {
                Vector2 endPos = transform.TransformPoint(points[points.Count - 1]);
                GameObject endSeg = CreateSegment(endPos, false);
                HingeJoint2D joint = endSeg.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousRB;
                joint.autoConfigureConnectedAnchor = true;
            }
        }

        private GameObject CreateSegment(Vector2 worldPos, bool isStatic)
        {
            GameObject seg = new GameObject("RopeSegment");
            seg.transform.position = worldPos;
            seg.transform.parent = transform;
            
            // Copy Logic
            seg.layer = gameObject.layer; 
            seg.tag = gameObject.tag;

            // --- PHYSICS ---
            Rigidbody2D rb = seg.AddComponent<Rigidbody2D>();
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = segmentMass;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            CapsuleCollider2D col = seg.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(ropeWidth, segmentLength);
            if (ropePhysicsMaterial != null) col.sharedMaterial = ropePhysicsMaterial;

            // --- VISUALS (Line Renderer per Segment) ---
            LineRenderer segLine = seg.AddComponent<LineRenderer>();
            
            // Copy material/colors from the main renderer settings
            if (_mainLineRenderer != null)
            {
                segLine.material = _mainLineRenderer.material;
                segLine.colorGradient = _mainLineRenderer.colorGradient;
                segLine.widthMultiplier = _mainLineRenderer.widthMultiplier;
                segLine.textureMode = _mainLineRenderer.textureMode;
                // Important: We usually want fewer vertices for straight segments
                segLine.numCapVertices = 0; 
                segLine.numCornerVertices = 0;
            }
            else
            {
                // Fallback if main renderer is missing
                segLine.startWidth = ropeWidth;
                segLine.endWidth = ropeWidth;
            }

            // Configure Local Space Drawing
            segLine.useWorldSpace = false; 
            segLine.positionCount = 2;
            
            // Calculate positions relative to center to match the Capsule Collider shape
            // Top of segment
            segLine.SetPosition(0, new Vector3(0, segmentLength / 2f, 0)); 
            // Bottom of segment
            segLine.SetPosition(1, new Vector3(0, -segmentLength / 2f, 0));

            return seg;
        }

        // NO UPDATE METHOD NEEDED! 
        // Since LineRenderers are local and children of the Rigidbody, they move with physics automatically.
    }
}#1#
*/

/*using UnityEngine;
using System.Collections.Generic;
using Drawing.LineControl;

namespace Drawing.Mechanics
{
    [RequireComponent(typeof(Line))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Rope Settings")]
        [Tooltip("Distance between physics segments. Lower values = smoother rope but higher performance cost.")]
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float segmentMass = 0.2f;
        [SerializeField] private float ropeWidth = 0.2f;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D ropePhysicsMaterial;

        // Component references
        private Line _lineComponent;
        private LineRenderer _lineRenderer;
        
        private List<Transform> _segments = new List<Transform>();
        private bool _isGenerated = false;

        private void Awake()
        {
            // Initialize references
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            // Subscribe to the event to know exactly when the user finishes drawing
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized += GenerateRopePhysics;
            }
        }

        private void OnDestroy()
        {
            // Always unsubscribe from events to prevent memory leaks
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized -= GenerateRopePhysics;
            }
        }

        /// <summary>
        /// Called automatically when the Line is finalized.
        /// Replaces the static/rigid physics with a chain of HingeJoints.
        /// </summary>
        private void GenerateRopePhysics()
        {
            // 1. Destroy the original rigid physics components created by the Line class
            // Using TryGetComponent to safely destroy only existing components
            if (TryGetComponent(out PolygonCollider2D poly)) Destroy(poly);
            if (TryGetComponent(out EdgeCollider2D edge)) Destroy(edge);
            if (TryGetComponent(out Rigidbody2D rb)) Destroy(rb);

            // 2. Retrieve drawing points
            List<Vector2> points = _lineComponent.points;
            
            // Validation: Ensure line is long enough
            if (points == null || points.Count < 2) return;

            // 3. Build the rope chain
            CreateChain(points);
            
            _isGenerated = true;
        }

        private void CreateChain(List<Vector2> points)
        {
            Rigidbody2D previousRB = null;

            // --- Create Anchor (Start Point) ---
            // The first point is static to hang the rope from the ceiling/wall
            Vector2 startPos = transform.TransformPoint(points[0]);
            GameObject anchor = CreateSegment(startPos, true);
            previousRB = anchor.GetComponent<Rigidbody2D>();
            _segments.Add(anchor.transform);

            float distanceAccumulator = 0f;
            Vector2 lastSegmentPos = startPos;

            // --- Create Dynamic Segments ---
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentPointWorld = transform.TransformPoint(points[i]);
                float dist = Vector2.Distance(lastSegmentPos, currentPointWorld);
                distanceAccumulator += dist;

                // Spawn a new segment only if we exceeded the defined segment length
                if (distanceAccumulator >= segmentLength)
                {
                    GameObject newSeg = CreateSegment(currentPointWorld, false);
                    
                    // Connect to the previous segment via HingeJoint2D
                    HingeJoint2D joint = newSeg.AddComponent<HingeJoint2D>();
                    joint.connectedBody = previousRB;
                    joint.autoConfigureConnectedAnchor = true;

                    previousRB = newSeg.GetComponent<Rigidbody2D>();
                    _segments.Add(newSeg.transform);
                    
                    lastSegmentPos = currentPointWorld;
                    distanceAccumulator = 0f;
                }
            }

            // --- Create End Segment ---
            // Ensure the rope reaches the exact end of the drawing
            if (distanceAccumulator > 0.01f) 
            {
                Vector2 endPos = transform.TransformPoint(points[points.Count - 1]);
                GameObject endSeg = CreateSegment(endPos, false);
                HingeJoint2D joint = endSeg.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousRB;
                joint.autoConfigureConnectedAnchor = true;
                _segments.Add(endSeg.transform);
            }
        }

        private GameObject CreateSegment(Vector2 worldPos, bool isStatic)
        {
            GameObject seg = new GameObject("RopeSegment");
            seg.transform.position = worldPos;
            seg.transform.parent = transform; 

            // --- Apply Parent's Layer and Tag ---
            seg.layer = gameObject.layer; 
            seg.tag = gameObject.tag;     

            // Configure Rigidbody
            Rigidbody2D rb = seg.AddComponent<Rigidbody2D>();
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = segmentMass;
                // Interpolation ensures smooth visual movement
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            // Add Physics Collider
            CapsuleCollider2D col = seg.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(ropeWidth, segmentLength);
            if (ropePhysicsMaterial != null) col.sharedMaterial = ropePhysicsMaterial;

            return seg;
        }

        private void Update()
        {
            // Sync the visual LineRenderer to the physical segment positions
            if (_isGenerated && _segments.Count > 0)
            {
                // Safety check in case segments were destroyed externally
                if (_segments[0] == null) 
                {
                    _isGenerated = false;
                    return;
                }

                _lineRenderer.positionCount = _segments.Count;
                _lineRenderer.useWorldSpace = true; // Must be World Space to match physics transforms

                for (int i = 0; i < _segments.Count; i++)
                {
                    _lineRenderer.SetPosition(i, _segments[i].position);
                }
            }
        }
    }
}


/*using UnityEngine;
using System.Collections.Generic;
using Drawing.LineControl;

namespace Drawing.Mechanics
{
    [RequireComponent(typeof(Line))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Rope Settings")]
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float segmentMass = 0.2f;
        [SerializeField] private float ropeWidth = 0.2f;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D ropePhysicsMaterial;

        private Line _lineComponent;
        private LineRenderer _lineRenderer;
        
        private List<Transform> _segments = new List<Transform>();
        private bool _isGenerated = false;

        private void Awake()
        {
            _lineComponent = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized += GenerateRopePhysics;
            }
        }

        private void OnDestroy()
        {
            if (_lineComponent != null)
            {
                _lineComponent.OnLineFinalized -= GenerateRopePhysics;
            }
        }

        private void GenerateRopePhysics()
        {

            if (TryGetComponent(out PolygonCollider2D poly)) Destroy(poly);
            if (TryGetComponent(out EdgeCollider2D edge)) Destroy(edge);
            if (TryGetComponent(out Rigidbody2D rb)) Destroy(rb);

            // 2. לקיחת הנקודות מהקומפוננטה ששמרנו
            List<Vector2> points = _lineComponent.points;
            
            // הגנה מפני קו קצר מדי
            if (points == null || points.Count < 2) return;

            // 3. יצירת החוליות
            CreateChain(points);
            
            _isGenerated = true;
        }

        private void CreateChain(List<Vector2> points)
        {
            Rigidbody2D previousRB = null;

            Vector2 startPos = transform.TransformPoint(points[0]);
            GameObject anchor = CreateSegment(startPos, true);
            previousRB = anchor.GetComponent<Rigidbody2D>();
            _segments.Add(anchor.transform);

            float distanceAccumulator = 0f;
            Vector2 lastSegmentPos = startPos;

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 currentPointWorld = transform.TransformPoint(points[i]);
                float dist = Vector2.Distance(lastSegmentPos, currentPointWorld);
                distanceAccumulator += dist;

                if (distanceAccumulator >= segmentLength)
                {
                    GameObject newSeg = CreateSegment(currentPointWorld, false);
                    
                    HingeJoint2D joint = newSeg.AddComponent<HingeJoint2D>();
                    joint.connectedBody = previousRB;
                    joint.autoConfigureConnectedAnchor = true;

                    previousRB = newSeg.GetComponent<Rigidbody2D>();
                    _segments.Add(newSeg.transform);
                    
                    lastSegmentPos = currentPointWorld;
                    distanceAccumulator = 0f;
                }
            }

            if (distanceAccumulator > 0.01f) 
            {
                Vector2 endPos = transform.TransformPoint(points[points.Count - 1]);
                GameObject endSeg = CreateSegment(endPos, false);
                HingeJoint2D joint = endSeg.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousRB;
                joint.autoConfigureConnectedAnchor = true;
                _segments.Add(endSeg.transform);
            }
        }

        private GameObject CreateSegment(Vector2 worldPos, bool isStatic)
        {
            GameObject seg = new GameObject("RopeSegment");
            seg.transform.position = worldPos;
            seg.transform.parent = transform; 
            seg.layer = gameObject.layer; 

            Rigidbody2D rb = seg.AddComponent<Rigidbody2D>();
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = segmentMass;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            CapsuleCollider2D col = seg.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(ropeWidth, segmentLength);
            if (ropePhysicsMaterial != null) col.sharedMaterial = ropePhysicsMaterial;

            return seg;
        }

        private void Update()
        {
            if (_isGenerated && _segments.Count > 0)
            {
                if (_segments[0] == null) 
                {
                    _isGenerated = false;
                    return;
                }

                _lineRenderer.positionCount = _segments.Count;
                _lineRenderer.useWorldSpace = true;

                for (int i = 0; i < _segments.Count; i++)
                {
                    _lineRenderer.SetPosition(i, _segments[i].position);
                }
            }
        }
    }
}
/*using UnityEngine;
using System.Collections.Generic;

namespace Drawing.LineControl
{
    [RequireComponent(typeof(Line))]
    public class RopeHandler : MonoBehaviour
    {
        [Header("Rope Settings")]
        [Tooltip("Distance between physics segments. Smaller = smoother but heavier.")]
        [SerializeField] private float segmentLength = 0.5f;
        [SerializeField] private float ropeWidth = 0.2f;
        [SerializeField] private float segmentMass = 0.5f;

        [Header("Physics Material")]
        [SerializeField] private PhysicsMaterial2D ropeMaterial;

        private Line _line;
        private List<Transform> _segments = new List<Transform>();
        private LineRenderer _lineRenderer;
        private bool _isInitialized = false;

        private void Start()
        {
            _line = GetComponent<Line>();
            _lineRenderer = GetComponent<LineRenderer>();

            // Wait one frame to ensure Line has finished its initialization
            Invoke(nameof(GenerateRope), 0.05f);
        }

        private void GenerateRope()
        {
            // 1. Remove the rigid physics components created by the standard Line system
            Destroy(GetComponent<PolygonCollider2D>());
            Destroy(GetComponent<Rigidbody2D>());
            Destroy(GetComponent<EdgeCollider2D>());

            // 2. Get points from the line (convert local to world)
            List<Vector2> originalPoints = _line.points;
            if (originalPoints == null || originalPoints.Count < 2) return;

            // 3. Create the chain
            Rigidbody2D previousRB = null;
            Vector2 previousPos = transform.TransformPoint(originalPoints[0]);

            // Create a static anchor at the start point (so the rope hangs from where you started drawing)
            GameObject anchor = CreateSegment(previousPos, true);
            previousRB = anchor.GetComponent<Rigidbody2D>();
            _segments.Add(anchor.transform);

            float distAccumulator = 0f;

            for (int i = 1; i < originalPoints.Count; i++)
            {
                Vector2 currentPos = transform.TransformPoint(originalPoints[i]);
                float dist = Vector2.Distance(previousPos, currentPos);
                distAccumulator += dist;

                // Create a segment only if we passed the segmentLength threshold
                if (distAccumulator >= segmentLength)
                {
                    GameObject newSegment = CreateSegment(currentPos, false);
                    
                    // Connect with HingeJoint
                    HingeJoint2D joint = newSegment.AddComponent<HingeJoint2D>();
                    joint.connectedBody = previousRB;
                    joint.autoConfigureConnectedAnchor = true;
                    
                    // Update references
                    previousRB = newSegment.GetComponent<Rigidbody2D>();
                    _segments.Add(newSegment.transform);
                    distAccumulator = 0f; // Reset counter
                }

                previousPos = currentPos;
            }

            // Add the final segment to ensure the rope reaches the end
            if (distAccumulator > 0.1f)
            {
                Vector2 lastPos = transform.TransformPoint(originalPoints[originalPoints.Count - 1]);
                GameObject endSeg = CreateSegment(lastPos, false);
                HingeJoint2D joint = endSeg.AddComponent<HingeJoint2D>();
                joint.connectedBody = previousRB;
                joint.autoConfigureConnectedAnchor = true;
                _segments.Add(endSeg.transform);
            }

            _isInitialized = true;
        }

        private GameObject CreateSegment(Vector2 position, bool isStatic)
        {
            // Create a new GameObject for this segment
            GameObject seg = new GameObject("RopeSegment");
            seg.transform.position = position;
            seg.transform.parent = transform; // Keep hierarchy clean
            seg.layer = gameObject.layer;     // Match the layer (e.g. "Line")

            // Add RB
            Rigidbody2D rb = seg.AddComponent<Rigidbody2D>();
            if (isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.mass = segmentMass;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement
            }

            // Add Collider (Capsule usually works best for ropes to avoid snagging)
            CapsuleCollider2D col = seg.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(ropeWidth, segmentLength);
            col.direction = CapsuleDirection2D.Vertical; // Or adjust based on flow
            if (ropeMaterial != null) col.sharedMaterial = ropeMaterial;

            return seg;
        }

        private void Update()
        {
            // 4. Update visual line to match physics segments
            if (_isInitialized && _segments.Count > 0)
            {
                _lineRenderer.positionCount = _segments.Count;
                for (int i = 0; i < _segments.Count; i++)
                {
                    // LineRenderer expects local positions if useWorldSpace is false
                    // But segments are children, so their localPosition is relative to the parent (Drawing)
                    _lineRenderer.SetPosition(i, _segments[i].localPosition);
                }
            }
        }
    }
}#3##2##1#*/
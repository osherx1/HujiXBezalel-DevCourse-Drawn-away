using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using Physics.Rock;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities.Camera.CameraShake;

namespace PhysicsObjects.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RockPhysicsController : MonoBehaviour
    {
        #region --- Configuration ---
        [Header("Lifecycle Settings")]
        [Tooltip("Time in seconds before the rock destroys itself automatically (if not broken). Set to 0 to disable.")]
        [SerializeField] private float maxLifeTime = 15.0f; // New: Self destruct timer for the main rock
        [Header("Thresholds")] [SerializeField]
        private float hardImpactThreshold = 5f;

        [SerializeField] private float shatterThreshold = 15f;

        [Header("Smart Velocity Capture")]
        [Tooltip("Layers representing obstacles (Ground, Player, etc.).")]
        [SerializeField]
        private LayerMask detectionLayers;

        [Tooltip("Start capturing velocity when the obstacle is within this distance (from the rock's edge).")]
        [SerializeField]
        private float maxDetectionDistance = 5.0f; 

        [Tooltip("Lock the velocity when the obstacle is closer than this distance.")] [SerializeField]
        private float minDetectionDistance = 0.5f;

        [Header("Visual Prefabs")] [SerializeField]
        private GameObject impactParticlePrefab;

        [SerializeField] private GameObject brokenRockPrefab;

        [Header("Audio & Physics")] [SerializeField]
        private GameSoundsSo.AudioType impactSound = GameSoundsSo.AudioType.RockHit;

        [Range(0f, 1f)] [SerializeField] private float impactVolume = 0.8f;
        [SerializeField] private GameSoundsSo.AudioType shatterSound = GameSoundsSo.AudioType.RockShatter;
        [Range(0f, 1f)] [SerializeField] private float shatterVolume = 1.0f;
        [SerializeField] private float explosionForce = 5f;
        [SerializeField] private float spinForce = 10f;
        
        [FormerlySerializedAs("rockShakeProfile")]
        [Header("Settings")]
        [SerializeField] private ShakeProfile heavyCameraShake;
        [SerializeField] private ShakeProfile shatterCameraShake;
        

        #endregion

        // --- Fields ---
        private ImpactCalculator _impactCalculator;
        private Rigidbody2D _rb;
        private Collider2D _collider;

        private Vector2 _capturedVelocity;
        private bool _hasCapturedVelocity = false;

        private RaycastHit2D[] _castResults = new RaycastHit2D[1];

        // --- Debug Fields ---
        private Color _gizmoColor = Color.green;
        private Vector2 _gizmoHitPosition;
        private bool _gizmoDidHit;

        // --- Unity Lifecycle ---

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _impactCalculator = new ImpactCalculator(hardImpactThreshold, shatterThreshold);
        }
        private void Start()
        {
            // Logic: Schedule self-destruction for the main rock
            if (maxLifeTime > 0)
            {
                Destroy(gameObject, maxLifeTime);
            }
        }

        private void FixedUpdate()
        {
            PerformVelocityCapture();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            float impactMagnitude =
                _hasCapturedVelocity ? _capturedVelocity.magnitude : collision.relativeVelocity.magnitude;
            ContactPoint2D contact = collision.GetContact(0);
            /*if(collision.collider.CompareTag("Line"))
            {
                Debug.Log("Rock collided with Line, impactMagnitude: " + impactMagnitude);
            }*/
            /*
            if(collision.collider.CompareTag("Player"))
            {
                Debug.Log("Rock collided with Player, impactMagnitude: " + impactMagnitude);
            }
            */

            if (_impactCalculator.IsShatterImpact(impactMagnitude) &&
                (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Line")||collision.collider.CompareTag("Player")))
            {
                Debug.Log("Shatter Impact Detected");
                PlaySound(shatterSound, shatterVolume);
                HandleShatter(contact.point);
                if (heavyCameraShake != null&&!collision.collider.CompareTag("Player"))
                {
                    EventManager.Instance.TriggerCameraShake(heavyCameraShake);
                }
                return; 
            }

            if (_impactCalculator.IsHardImpact(impactMagnitude))
            {
                PlaySound(impactSound, impactVolume);
                SpawnImpactParticles(contact.point);
                _hasCapturedVelocity = false;
                if (shatterCameraShake != null)
                {
                    EventManager.Instance.TriggerCameraShake(shatterCameraShake);
                }
                
            }
        }

        // --- Core Logic ---

        private void PerformVelocityCapture()
        {
            Vector2 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;

            if (speed < 0.1f)
            {
                _gizmoColor = Color.gray; // Idle
                return;
            }

            Vector2 direction = velocity.normalized;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(detectionLayers);
            filter.useTriggers = false;

            int hitCount = _rb.Cast(direction, filter, _castResults, maxDetectionDistance);

            if (hitCount > 0)
            {
                RaycastHit2D hit = _castResults[0];
                float distanceToObstacle = hit.distance;

                // Debug Update
                _gizmoDidHit = true;
                _gizmoHitPosition = hit.point;

                // CASE A: Yellow Zone (Capturing)
                if (distanceToObstacle <= maxDetectionDistance && distanceToObstacle > minDetectionDistance)
                {
                    _capturedVelocity = velocity;
                    _hasCapturedVelocity = true;
                    _gizmoColor = Color.yellow; // Visual: Warning / Capturing
                }
                // CASE B: Red Zone (Locked)
                else
                {
                    // Locked - keeping previous captured velocity
                    _gizmoColor = Color.red; // Visual: Danger / Locked
                }
            }
            else
            {
                // CASE C: Green Zone (Safe)
                _hasCapturedVelocity = false;
                _gizmoDidHit = false;
                _gizmoColor = Color.green; // Visual: Safe / Scanning
            }
        }

        private void HandleShatter(Vector2 position)
        {
            SpawnImpactParticles(position);

            if (brokenRockPrefab != null)
            {
                GameObject brokenInstance = Instantiate(brokenRockPrefab, transform.position, transform.rotation);
                ApplyExplosionPhysics(brokenInstance);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactParticles(Vector2 position)
        {
            if (impactParticlePrefab != null)
                Instantiate(impactParticlePrefab, position, Quaternion.identity);
        }

        private void ApplyExplosionPhysics(GameObject brokenRoot)
        {
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();
            Vector2 explosionCenter = transform.position;
            Vector2 momentumToTransfer = _hasCapturedVelocity ? _capturedVelocity : _rb.linearVelocity;

            foreach (Rigidbody2D pieceRb in pieces)
            {
                pieceRb.linearVelocity = momentumToTransfer;
                Vector2 direction = (pieceRb.position - explosionCenter).normalized;
                direction += Random.insideUnitCircle * 0.5f; 

                pieceRb.AddForce(direction * explosionForce, ForceMode2D.Impulse);
                float randomSpin = Random.Range(-1f, 1f) * spinForce;
                pieceRb.AddTorque(randomSpin, ForceMode2D.Impulse);
            }
        }

 
        private void PlaySound(GameSoundsSo.AudioType audioType, float volume = 1f)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySoundByAudioType(audioType, volume);
        }
        // --- Debug Gizmos ---

        private void OnDrawGizmos()
        {
            // Only draw when playing to see live data, or draw basic range in editor
            if (!Application.isPlaying)
            {
                DrawEditorGizmos();
                return;
            }

            Gizmos.color = _gizmoColor;

            // Draw the direction ray based on actual velocity
            if (_rb != null)
            {
                Vector2 origin = transform.position;
                Vector2 direction = _rb.linearVelocity.normalized;
                
                // Draw Cast Line
                float drawDist = _gizmoDidHit ? Vector2.Distance(origin, _gizmoHitPosition) : maxDetectionDistance;
                Gizmos.DrawLine(origin, origin + (direction * drawDist));

                // Draw Hit Point
                if (_gizmoDidHit)
                {
                    Gizmos.DrawWireSphere(_gizmoHitPosition, 0.2f);
                }
            }
        }

        private void DrawEditorGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, minDetectionDistance);
            Gizmos.color = new Color(1, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, maxDetectionDistance);
        }




        public void AddTorqueForces( float launchTorque, ForceMode2D forceMode)
        {
            if (_rb != null)
            {
                Debug.Log("AddTorqueForces");
                _rb.AddTorque(launchTorque, forceMode);
            }
            else
            {
                Debug.LogWarning("RockPhysicsController: Rigidbody2D is null, cannot apply torque.");
            }
        }
    }
}


/*using Drawing.Data;
using Drawing.Managers.Core.Managers;
using Physics.Rock;
using UnityEngine;

namespace PhysicsObjects.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RockPhysicsController : MonoBehaviour
    {
        #region --- Configuration ---

        [Header("Thresholds")] [SerializeField]
        private float hardImpactThreshold = 5f;

        [SerializeField] private float shatterThreshold = 15f;

        [Header("Smart Velocity Capture")]
        [Tooltip("Layers representing obstacles (Ground, Player, etc.).")]
        [SerializeField]
        private LayerMask detectionLayers;

        [Tooltip("Start capturing velocity when the obstacle is within this distance (from the rock's edge).")]
        [SerializeField]
        private float maxDetectionDistance = 5.0f; // Adjusted for size 5 object

        [Tooltip("Lock the velocity when the obstacle is closer than this distance.")] [SerializeField]
        private float minDetectionDistance = 0.5f; // Adjusted for size 5 object

        [Header("Visual Prefabs")] [SerializeField]
        private GameObject impactParticlePrefab;

        [SerializeField] private GameObject brokenRockPrefab;

        [Header("Audio & Physics")] [SerializeField]
        private GameSoundsSo.AudioType impactSound = GameSoundsSo.AudioType.RockHit;

        [Range(0f, 1f)] [SerializeField] private float impactVolume = 0.8f;
        [SerializeField] private GameSoundsSo.AudioType shatterSound = GameSoundsSo.AudioType.RockShatter;
        [Range(0f, 1f)] [SerializeField] private float shatterVolume = 1.0f;
        [SerializeField] private float explosionForce = 5f;
        [SerializeField] private float spinForce = 10f;

        #endregion

        // --- Fields ---
        private ImpactCalculator _impactCalculator;
        private Rigidbody2D _rb;
        private Collider2D _collider;

        private Vector2 _capturedVelocity;
        private bool _hasCapturedVelocity = false;

        // Array to store Cast results (avoids garbage allocation)
        private RaycastHit2D[] _castResults = new RaycastHit2D[1];

        // --- Unity Lifecycle ---

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _impactCalculator = new ImpactCalculator(hardImpactThreshold, shatterThreshold);
        }

        private void FixedUpdate()
        {
            PerformVelocityCapture();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Logic: Use captured velocity if available (for better momentum), 
            // otherwise fallback to the physics engine's relative velocity.
            float impactMagnitude =
                _hasCapturedVelocity ? _capturedVelocity.magnitude : collision.relativeVelocity.magnitude;
            ContactPoint2D contact = collision.GetContact(0);

            // 1. Check for Shatter (High Priority)
            if (_impactCalculator.IsShatterImpact(impactMagnitude) &&
                (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Line")))
            {
                PlaySound(shatterSound, shatterVolume);
                HandleShatter(contact.point);
                return; // Stop execution, object is destroyed
            }

            // 2. Check for Hard Impact (Low Priority)
            if (_impactCalculator.IsHardImpact(impactMagnitude))
            {
                PlaySound(impactSound, impactVolume);
                SpawnImpactParticles(contact.point);

                // Reset capture so we can detect the next impact if we didn't break
                _hasCapturedVelocity = false;
            }
        }

        // --- Core Logic ---

        /// <summary>
        /// Uses Rigidbody2D.Cast to project the rock's shape forward.
        /// It captures the velocity if an obstacle is within the "Yellow Zone" (Max > Dist > Min).
        /// </summary>
        private void PerformVelocityCapture()
        {
            Vector2 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;

            // Optimization: Don't calculate if barely moving
            if (speed < 0.1f) return;

            Vector2 direction = velocity.normalized;

            // Prepare the Cast filter
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(detectionLayers);
            filter.useTriggers = false; // Ignore trigger colliders

            // Perform the Cast. This projects the collider shape forward.
            // Result is stored in _castResults.
            int hitCount = _rb.Cast(direction, filter, _castResults, maxDetectionDistance);

            if (hitCount > 0)
            {
                RaycastHit2D hit = _castResults[0];
                float distanceToObstacle = hit.distance;

                // --- ZONE LOGIC ---

                // CASE A: Yellow Zone (Capturing)
                // The obstacle is seen, but not yet too close. Keep updating velocity.
                if (distanceToObstacle <= maxDetectionDistance && distanceToObstacle > minDetectionDistance)
                {
                    _capturedVelocity = velocity;
                    _hasCapturedVelocity = true;
                }
                // CASE B: Red Zone (Locked)
                // The obstacle is very close. Stop updating to prevent capturing "slow" physics frames.
                // We keep the value stored from Case A.
            }
            else
            {
                // CASE C: Green Zone (Safe)
                // Nothing ahead.
                _hasCapturedVelocity = false;
            }
        }

        private void HandleShatter(Vector2 position)
        {
            SpawnImpactParticles(position);

            if (brokenRockPrefab != null)
            {
                GameObject brokenInstance = Instantiate(brokenRockPrefab, transform.position, transform.rotation);
                ApplyExplosionPhysics(brokenInstance);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactParticles(Vector2 position)
        {
            if (impactParticlePrefab != null)
                Instantiate(impactParticlePrefab, position, Quaternion.identity);
        }

        private void ApplyExplosionPhysics(GameObject brokenRoot)
        {
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();
            Vector2 explosionCenter = transform.position;

            // Use the captured velocity to maintain natural momentum
            Vector2 momentumToTransfer = _hasCapturedVelocity ? _capturedVelocity : _rb.linearVelocity;

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // 1. Inherit Velocity
                pieceRb.linearVelocity = momentumToTransfer;

                // 2. Explosion Direction
                Vector2 direction = (pieceRb.position - explosionCenter).normalized;
                direction += Random.insideUnitCircle * 0.5f; // Add noise

                // 3. Apply Forces
                pieceRb.AddForce(direction * explosionForce, ForceMode2D.Impulse);
                float randomSpin = Random.Range(-1f, 1f) * spinForce;
                pieceRb.AddTorque(randomSpin, ForceMode2D.Impulse);
            }
        }

        private void PlaySound(GameSoundsSo.AudioType audioType, float volume = 1f)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySoundByAudioType(audioType, volume);
        }

        // --- Visualization (Gizmos) ---
        private void OnDrawGizmos()
        {
            if (_rb == null || _collider == null) return;

            Vector2 velocity = _rb.linearVelocity;
            Vector2 direction = velocity.normalized;
            if (direction == Vector2.zero) direction = Vector2.down;

            // Calculate visuals based on Collider size
            float approxRadius = _collider.bounds.extents.x;
            Vector2 center = transform.position;
            Vector2 edgeOrigin = center + (direction * approxRadius); // Start drawing from the edge

            // Default: Safe state (Green)
            Color stateColor = Color.green;
            float drawDistance = maxDetectionDistance; // Draw full length by default

            // --- SIMULATE RAYCAST FOR VISUALS ---
            RaycastHit2D hit =
                Physics2D.CircleCast(center, approxRadius, direction, maxDetectionDistance, detectionLayers);

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                // Distance from center minus radius = Distance from edge (Approx)
                float distFromEdge = hit.distance;

                // 1. Determine Color based on Zones
                if (distFromEdge <= minDetectionDistance)
                {
                    stateColor = Color.red; // LOCKED
                }
                else if (distFromEdge <= maxDetectionDistance)
                {
                    stateColor = Color.yellow; // CAPTURING
                }

                // 2. Shorten the line to the obstacle
                drawDistance = distFromEdge;
            }

            // --- DRAWING ---

            // 1. Draw Threshold Markers (Ghost Lines) - Always visible so you know the ranges
            Gizmos.color = new Color(1, 1, 0, 0.2f); // Faint Yellow
            Gizmos.DrawWireSphere(edgeOrigin + (direction * maxDetectionDistance), approxRadius); // Max Range

            Gizmos.color = new Color(1, 0, 0, 0.2f); // Faint Red
            Gizmos.DrawWireSphere(edgeOrigin + (direction * minDetectionDistance), approxRadius); // Min Range


            // 2. Draw The Active Beam (Changes Color & Length)
            Gizmos.color = stateColor;

            Vector2 endPoint = edgeOrigin + (direction * drawDistance);

            // Draw Line
            Gizmos.DrawLine(edgeOrigin, endPoint);

            // Draw End Sphere (The "Head" of the sensor)
            Gizmos.DrawWireSphere(endPoint, approxRadius);

            // 3. Draw Connection Lines (To visualize the thickness/radius)
            // Calculate perpendicular vector for thickness
            Vector2 perp = Vector2.Perpendicular(direction) * approxRadius;
            Gizmos.DrawLine(edgeOrigin + perp, endPoint + perp);
            Gizmos.DrawLine(edgeOrigin - perp, endPoint - perp);
        }
    }
}*/
/*private void OnDrawGizmos()
{
    // Safety check
    if (_rb == null || _collider == null) return;

    Vector2 velocity = _rb.linearVelocity;
    Vector2 direction = velocity.normalized;

    // Default direction for Editor view (when not playing)
    if (direction == Vector2.zero) direction = Vector2.down;

    // Calculate origins
    // Rigidbody.Cast measures from the edge, so we approximate the edge for the Gizmo.
    // Using Bounds extends works well for circles/boxes.
    float approxRadius = _collider.bounds.extents.x;
    Vector2 center = transform.position;
    Vector2 edgeOrigin = center + (direction * approxRadius);

    // Define points
    Vector2 maxPoint = edgeOrigin + (direction * maxDetectionDistance);
    Vector2 minPoint = edgeOrigin + (direction * minDetectionDistance);

    // --- Determine Gizmo Color ---
    Color gizmoColor = Color.green; // Default: Safe / Searching

    // To visualize correctly in Editor, we simulate the Cast roughly
    // Note: Physics2D.CircleCast is used here for visualization approximation.
    // In Play Mode, the real logic uses _rb.Cast which is more accurate for polygons.
    RaycastHit2D hit = Physics2D.CircleCast(center, approxRadius, direction, maxDetectionDistance, detectionLayers);

    if (hit.collider != null && hit.collider.gameObject != gameObject)
    {
        // Calculate distance from edge (approx)
        float distFromEdge = hit.distance;

        if (distFromEdge <= minDetectionDistance)
        {
            gizmoColor = Color.red; // Locked! Impact Imminent.
        }
        else if (distFromEdge <= maxDetectionDistance)
        {
            gizmoColor = Color.yellow; // Capturing velocity...
        }
    }

    // --- Draw ---
    Gizmos.color = gizmoColor;

    // 1. Draw the "Sensor Beam" line from edge to max distance
    Gizmos.DrawLine(edgeOrigin, maxPoint);

    // 2. Draw the 'End' sphere (Max Range)
    Gizmos.DrawWireSphere(maxPoint, 0.2f);

    // 3. Draw the 'Lock' sphere (Min Range)
    // Make it solid if locked (Red), transparent otherwise
    if (gizmoColor == Color.red)
    {
         Gizmos.DrawSphere(minPoint, 0.2f);
    }
    else
    {
         Gizmos.color = new Color(1, 0, 0, 0.5f); // Semi-transparent red
         Gizmos.DrawWireSphere(minPoint, 0.2f);
    }
}*/


/*
using Drawing.Data;
using Drawing.Managers.Core.Managers;
using Physics.Rock;
using UnityEngine;

namespace PhysicsObjects.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RockPhysicsController : MonoBehaviour
    {
        #region --- Configuration ---

        [Header("Thresholds")]
        [SerializeField] private float hardImpactThreshold = 5f;
        [SerializeField] private float shatterThreshold = 15f;

        [Header("Smart Velocity Capture (CircleCast)")]
        [Tooltip("Layers that trigger velocity capture (Ground, Player, etc.).")]
        [SerializeField] private LayerMask detectionLayers;

        [Tooltip("The radius of the detection circle (should match rock size approximately).")]
        [SerializeField] private float detectionRadius = 0.5f;

        [Tooltip("Start capturing velocity when obstacle is within this distance.")]
        [SerializeField] private float maxDetectionDistance = 2.0f;

        [Tooltip("Stop updating (LOCK) velocity when obstacle is closer than this distance.")]
        [SerializeField] private float minDetectionDistance = 0.2f;

        [Header("Visual Prefabs")]
        [SerializeField] private GameObject impactParticlePrefab;
        [SerializeField] private GameObject brokenRockPrefab;

        [Header("Audio Settings")]
        [SerializeField] private GameSoundsSo.AudioType impactSound = GameSoundsSo.AudioType.RockHit;
        [Range(0f, 1f)] [SerializeField] private float impactVolume = 0.8f;

        [Space(10)]

        [SerializeField] private GameSoundsSo.AudioType shatterSound = GameSoundsSo.AudioType.RockShatter;
        [Range(0f, 1f)] [SerializeField] private float shatterVolume = 1.0f;

        [Header("Explosion Physics")]
        [SerializeField] private float explosionForce = 5f;
        [SerializeField] private float spinForce = 10f;

        #endregion

        // --- State Variables ---
        private ImpactCalculator _impactCalculator;
        private Rigidbody2D _rb;

        private Vector2 _capturedVelocity;
        private bool _hasCapturedVelocity = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _impactCalculator = new ImpactCalculator(hardImpactThreshold, shatterThreshold);
        }

        private void FixedUpdate()
        {
            PerformVelocityCapture();
        }

        /// <summary>
        /// Casts a circle forward to detect obstacles and capture velocity intelligently.
        /// </summary>
        private void PerformVelocityCapture()
        {
            // 1. Calculate movement direction
            Vector2 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;
            Vector2 direction = velocity.normalized;

            // Don't trace if not moving significantly
            if (speed < 0.1f) return;

            // 2. Perform CircleCast
            // Origin, Radius, Direction, MaxDistance, LayerMask
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, detectionRadius, direction, maxDetectionDistance, detectionLayers);

            if (hit.collider != null)
            {
                float distanceToObstacle = hit.distance; // Distance from the edge of the circle cast to the hit point

                // CASE A: Inside the "Capture Zone" (Between Max and Min)
                // We continuously update the captured velocity to get the freshest, fastest value.
                if (distanceToObstacle <= maxDetectionDistance && distanceToObstacle > minDetectionDistance)
                {
                    _capturedVelocity = velocity;
                    _hasCapturedVelocity = true;
                }
                // CASE B: Too Close (Closer than Min)
                // We STOP updating. We lock the value we captured in Case A.
                // This prevents capturing the physics engine's "slow down" frame right at impact.
            }
            else
            {
                // Reset capture if we are flying freely (e.g., fell off a ledge)
                _hasCapturedVelocity = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Use captured velocity if available, otherwise fallback to relative velocity
            float impactMagnitude = _hasCapturedVelocity ? _capturedVelocity.magnitude : collision.relativeVelocity.magnitude;
            ContactPoint2D contact = collision.GetContact(0);

            // 1. Check for Shatter
            if (_impactCalculator.IsShatterImpact(impactMagnitude) &&
                (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Line")))
            {
                PlaySound(shatterSound, shatterVolume);
                HandleShatter(contact.point);
                return;
            }

            // 2. Check for Hard Impact
            if (_impactCalculator.IsHardImpact(impactMagnitude))
            {
                PlaySound(impactSound, impactVolume);
                SpawnImpactParticles(contact.point);

                // Reset flag to allow new capture for next bounce
                _hasCapturedVelocity = false;
            }
        }

        private void HandleShatter(Vector2 position)
        {
            SpawnImpactParticles(position);

            if (brokenRockPrefab != null)
            {
                GameObject brokenInstance = Instantiate(brokenRockPrefab, transform.position, transform.rotation);
                ApplyExplosionPhysics(brokenInstance);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactParticles(Vector2 position)
        {
            if (impactParticlePrefab != null)
            {
                Instantiate(impactParticlePrefab, position, Quaternion.identity);
            }
        }

        private void ApplyExplosionPhysics(GameObject brokenRoot)
        {
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();
            Vector2 explosionCenter = transform.position;

            // Use the captured velocity to maintain momentum
            Vector2 momentumToTransfer = _hasCapturedVelocity ? _capturedVelocity : _rb.linearVelocity;

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // Inherit Velocity
                pieceRb.linearVelocity = momentumToTransfer;

                // Explosion logic
                Vector2 direction = (pieceRb.position - explosionCenter).normalized;
                direction += Random.insideUnitCircle * 0.5f;

                pieceRb.AddForce(direction * explosionForce, ForceMode2D.Impulse);

                float randomSpin = Random.Range(-1f, 1f) * spinForce;
                pieceRb.AddTorque(randomSpin, ForceMode2D.Impulse);
            }
        }

        private void PlaySound(GameSoundsSo.AudioType audioType, float volume = 1f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySoundByAudioType(audioType, volume);
            }
        }

        // --- Improved Gizmos ---
        // --- Dynamic Gizmos ---
        private void OnDrawGizmos()
        {
            if (_rb == null) return;

            Vector2 velocity = _rb.linearVelocity;
            Vector2 direction = velocity.normalized;

            // Default downward if stationary (for editing)
            if (direction == Vector2.zero) direction = Vector2.down;

            Vector2 origin = transform.position;
            Vector2 maxPoint = origin + (direction * maxDetectionDistance);
            Vector2 minPoint = origin + (direction * minDetectionDistance);

            // --- Real-time Detection Logic for Gizmo Colors ---
            // We run a simulation raycast here just for the visuals
            RaycastHit2D hit = Physics2D.CircleCast(origin, detectionRadius, direction, maxDetectionDistance, detectionLayers);

            Color gizmoColor = Color.green; // Default: Safe / Nothing detected

            if (hit.collider != null)
            {
                float dist = hit.distance;

                if (dist <= minDetectionDistance)
                {
                    gizmoColor = Color.red; // Locked / Too Close
                }
                else if (dist <= maxDetectionDistance)
                {
                    gizmoColor = Color.yellow; // Capturing velocity
                }

                // Draw the actual hit point
                Gizmos.color = gizmoColor;
                Vector2 hitCenter = origin + (direction * hit.distance);
                Gizmos.DrawWireSphere(hitCenter, detectionRadius); // Where the rock will "touch"
            }

            // --- Draw The Visualization ---
            Gizmos.color = gizmoColor;

            // 1. Draw the "Tunnel" lines
            Vector2 perp = Vector2.Perpendicular(direction) * detectionRadius;
            Gizmos.DrawLine(origin + perp, maxPoint + perp);
            Gizmos.DrawLine(origin - perp, maxPoint - perp);

            // 2. Draw Max Range Sphere
            // If we are yellow or red, the max range stays colored to indicate awareness
            Gizmos.DrawWireSphere(maxPoint, detectionRadius);

            // 3. Draw Min Range Sphere (The "Lock" line)
            // Make this slightly transparent if not active, or solid if red
            Gizmos.color = (gizmoColor == Color.red) ? Color.red : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(minPoint, detectionRadius);
        }
    }
}
        /*private void OnDrawGizmos()
        {
            if (_rb == null) return;

            Vector2 direction = _rb.linearVelocity.normalized;
            if (direction == Vector2.zero) direction = Vector2.down; // Default for editor view

            Vector2 origin = transform.position;

            // 1. Draw Max Range (Yellow) - Detection starts here
            Gizmos.color = Color.yellow;
            Vector2 maxPoint = origin + (direction * maxDetectionDistance);
            Gizmos.DrawWireSphere(maxPoint, detectionRadius);
            Gizmos.DrawLine(origin, maxPoint);

            // 2. Draw Min Range (Red) - Velocity LOCKS here
            Gizmos.color = Color.red;
            Vector2 minPoint = origin + (direction * minDetectionDistance);
            Gizmos.DrawWireSphere(minPoint, detectionRadius);

            // 3. Draw Connection Line
            Gizmos.color = new Color(1, 1, 1, 0.3f);
            Gizmos.DrawLine(minPoint, maxPoint);
        }#1#
        */


/*using Drawing.Data;
using Drawing.Managers.Core.Managers;
using Physics.Rock;
using UnityEngine;
// For GameSoundsSo.AudioType

// For AudioManager

namespace PhysicsObjects.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RockPhysicsController : MonoBehaviour
    {
        [Header("Thresholds")]
        [SerializeField] private float hardImpactThreshold = 5f;
        [SerializeField] private float shatterThreshold = 15f;

        [Header("Visual Prefabs")]
        [SerializeField] private GameObject impactParticlePrefab;
        [SerializeField] private GameObject brokenRockPrefab;

        [Header("Audio Settings")]
        [Tooltip("Sound for hard impact.")]
        [SerializeField] private GameSoundsSo.AudioType impactSound = GameSoundsSo.AudioType.RockHit;
        [Tooltip("Volume for hard impact (0-1).")]
        [Range(0f, 1f)] [SerializeField] private float impactVolume = 0.8f;

        [Space(10)]

        [Tooltip("Sound for shattering.")]
        [SerializeField] private GameSoundsSo.AudioType shatterSound = GameSoundsSo.AudioType.RockShatter;
        [Tooltip("Volume for shattering (0-1).")]
        [Range(0f, 1f)] [SerializeField] private float shatterVolume = 1.0f;


        [Header("Explosion Physics")]
        [SerializeField] private float explosionForce = 5f;
        [SerializeField] private float spinForce = 10f;

        private ImpactCalculator _impactCalculator;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _impactCalculator = new ImpactCalculator(hardImpactThreshold, shatterThreshold);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            float impactVelocity = collision.relativeVelocity.magnitude;
            ContactPoint2D contact = collision.GetContact(0);

            // 1. Check for Shatter (Highest Priority)
            if (_impactCalculator.IsShatterImpact(impactVelocity) &&
                (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Line")))
            {
                PlaySound(shatterSound,shatterVolume); // Play shatter sound
                HandleShatter(contact.point);
                return; // Stop execution (object is broken)
            }

            // 2. Check for Hard Impact (Secondary Priority)
            if (_impactCalculator.IsHardImpact(impactVelocity))
            {
                PlaySound(impactSound, impactVolume);
                SpawnImpactParticles(contact.point);
            }
        }

        private void HandleShatter(Vector2 position)
        {
            if (brokenRockPrefab != null)
            {
                GameObject brokenInstance = Instantiate(brokenRockPrefab, transform.position, transform.rotation);
                ApplyExplosionPhysics(brokenInstance);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactParticles(Vector2 position)
        {
            if (impactParticlePrefab != null)
            {
                Instantiate(impactParticlePrefab, position, Quaternion.identity);
            }
        }

        private void ApplyExplosionPhysics(GameObject brokenRoot)
        {
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();
            Vector2 explosionCenter = transform.position;

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // 1. Inherit Velocity
                pieceRb.linearVelocity = _rb.linearVelocity;

                // 2. Calculate Direction & Randomness
                Vector2 direction = (pieceRb.position - explosionCenter).normalized;
                direction += Random.insideUnitCircle * 0.5f;

                // 3. Apply Force & Spin
                pieceRb.AddForce(direction * explosionForce, ForceMode2D.Impulse);

                float randomSpin = Random.Range(-1f, 1f) * spinForce;
                pieceRb.AddTorque(randomSpin, ForceMode2D.Impulse);
            }
        }


        private void PlaySound(GameSoundsSo.AudioType audioType, float volume=1f)
        {
            if (AudioManager.Instance != null)
            {
                // The existing AudioManager supports volume scaling!
                AudioManager.Instance.PlaySoundByAudioType(audioType, volume);
            }
        }
    }
}
/*using Drawing.Data;
using Drawing.Managers.Core.Managers;
using UnityEngine;

namespace Physics.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RockPhysicsController : MonoBehaviour
    {
        [Header("Thresholds")] [SerializeField]
        private float hardImpactThreshold = 5f;

        [SerializeField] private float shatterThreshold = 15f;

        [Header("Prefabs")] [SerializeField] private GameObject impactParticlePrefab;
        [SerializeField] private GameObject brokenRockPrefab; // Prefab with pieces

        private ImpactCalculator _impactCalculator;
        private Rigidbody2D _rb;
        [Header("Explosion Physics")]
        [SerializeField]private float explosionForce = 5f;
        [SerializeField]private float spinForce =10f;
        [Header("Audio Settings")]
        [Tooltip("Sound to play when the rock hits something hard but doesn't break.")]
        [SerializeField] private GameSoundsSo.AudioType impactSound = GameSoundsSo.AudioType.RockHit;

        [Tooltip("Sound to play when the rock shatters.")]
        [SerializeField] private GameSoundsSo.AudioType shatterSound = GameSoundsSo.AudioType.RockShatter;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _impactCalculator = new ImpactCalculator(hardImpactThreshold, shatterThreshold);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            float impactVelocity = collision.relativeVelocity.magnitude;
            ContactPoint2D contact = collision.GetContact(0);

            // 1. Check for Shatter (High Priority)
            if (_impactCalculator.IsShatterImpact(impactVelocity) &&
                (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Line")))
            {
                HandleShatter(contact.point);
                return; // Stop here, object is destroyed
            }

            // 2. Check for Hard Impact (Low Priority)
            if (_impactCalculator.IsHardImpact(impactVelocity))
            {
                HandleHardImpact(contact.point);
            }
        }

        private void HandleHardImpact(Vector2 hitPosition)
        {
            if (impactParticlePrefab != null)
            {
                Instantiate(impactParticlePrefab, hitPosition, Quaternion.identity);
            }
        }

        private void HandleShatter(Vector2 hitPosition)
        {
            // Add impact effect even when shattering
            HandleHardImpact(hitPosition);

            if (brokenRockPrefab != null)
            {
                // Spawn the broken version at current position/rotation
                GameObject brokenInstance = Instantiate(brokenRockPrefab, transform.position, transform.rotation);

                // Apply current velocity to the broken pieces so they fly naturally
                ApplyExplosionPhysics(brokenInstance);
            }

            // Destroy the whole rock
            Destroy(gameObject);
        }
        private void PlaySound(GameSoundsSo.AudioType audioType)
        {
            if (AudioManager.Instance != null)
            {
                // Optional: You can pass impact velocity as volume scale if you want dynamic volume
                AudioManager.Instance.PlaySoundByAudioType(audioType);
            }
        }

        private void ApplyMomentumToPieces(GameObject brokenRoot)
        {
            // Get all rigidbodies in the broken prefab children
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // Transfer velocity + add a little explosion force effect outward
                pieceRb.linearVelocity = _rb.linearVelocity;
                pieceRb.angularVelocity = _rb.angularVelocity;
            }
        }

        private void ApplyExplosionPhysics(GameObject brokenRoot)
        {
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();
            Vector2 explosionCenter = transform.position;

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // 1. Inherit Velocity
                pieceRb.linearVelocity = _rb.linearVelocity;

                // 2. Calculate Direction
                Vector2 direction = (pieceRb.position - explosionCenter).normalized;

                // 3. Add Randomness
                // Random.insideUnitCircle
                direction += Random.insideUnitCircle * 0.5f;

                // 4. Apply Explosion Force
                pieceRb.AddForce(direction * explosionForce, ForceMode2D.Impulse);

                // 5. Apply Random Spin
                float randomSpin = Random.Range(-1f, 1f) * spinForce;
                pieceRb.AddTorque(randomSpin, ForceMode2D.Impulse);
            }
        }
    }
}#1#*/
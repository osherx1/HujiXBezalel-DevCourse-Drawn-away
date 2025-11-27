namespace Prototype1
{
using UnityEngine;

namespace Prototype1
{
    public class SecurityCamera : MonoBehaviour
    {
        #region Settings - Rotation (Scan)
        [Header("Scan Settings")]
        [SerializeField] private float rotationSpeed = 20f; // Degrees per second
        [Tooltip("The minimum angle (e.g., -45 for looking left/down)")]
        [SerializeField] private float minAngle = -45f;
        [Tooltip("The maximum angle (e.g., 45 for looking right/down)")]
        [SerializeField] private float maxAngle = 45f;
        [SerializeField] private float waitTimeAtEdge = 2f;
        #endregion

        #region Settings - Randomization
        [Header("Randomization")]
        [SerializeField] private float minStartDelay = 0f;
        [SerializeField] private float maxStartDelay = 2f;
        [SerializeField] private bool randomizeStartDirection = true;
        #endregion

        #region Settings - Detection
        [Header("Detection Settings")]
        [SerializeField] private float viewDistance = 8f;
        [SerializeField] private float viewAngle = 30f; // The width of the beam itself
        [SerializeField] private float timeToCatch = 1.0f; // Cameras usually detect faster
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer beamSprite;
        [SerializeField] private Color safeColor = new Color(1f, 1f, 0, 0.3f);
        [SerializeField] private Color alertColor = new Color(1f, 0, 0, 0.5f);
        #endregion

        #region Private Variables
        [SerializeField] private Transform player;
        
        private float _targetAngle;
        private float _currentAngle;
        private float _waitTimer;
        private float _suspicionTimer;
        private float _startDelayTimer;
        
        private bool _isRotatingToMax = true; // Direction flag
        private bool _isAlerted = false;
        #endregion

        private void Start()
        {
            // Initialize logic
            InitializeRandomScan();
        }

        private void Update()
        {
            if (player == null) return;

            // Handle start delay
            if (_startDelayTimer > 0)
            {
                _startDelayTimer -= Time.deltaTime;
                return;
            }

            bool canSeePlayer = CheckPlayerInSight();

            if (canSeePlayer)
            {
                HandleAlert();
            }
            else
            {
                HandleScan();
            }

            UpdateVisuals(canSeePlayer);
        }

        #region Core Logic

        private void InitializeRandomScan()
        {
            // 1. Random delay before starting
            _startDelayTimer = Random.Range(minStartDelay, maxStartDelay);

            // 2. Start at a random angle within range
            _currentAngle = Random.Range(minAngle, maxAngle);
            ApplyRotation(_currentAngle);

            // 3. Randomize initial direction
            if (randomizeStartDirection)
            {
                _isRotatingToMax = (Random.value > 0.5f);
            }

            // 4. Set initial target
            _targetAngle = _isRotatingToMax ? maxAngle : minAngle;
        }

        private void HandleAlert()
        {
            _isAlerted = true;
            // Stop rotating when player is spotted
            _suspicionTimer += Time.deltaTime;

            if (_suspicionTimer >= timeToCatch)
            {
                GameOver();
            }
        }

        private void HandleScan()
        {
            // Cooldown if previously alerted
            if (_suspicionTimer > 0)
            {
                _suspicionTimer -= Time.deltaTime;
                return;
            }
            _isAlerted = false;

            // Check if we reached the target angle (with small buffer)
            if (Mathf.Abs(_currentAngle - _targetAngle) < 0.1f)
            {
                // Wait at edge
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= waitTimeAtEdge)
                {
                    SwitchScanDirection();
                }
            }
            else
            {
                // Rotate towards target
                _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, rotationSpeed * Time.deltaTime);
                ApplyRotation(_currentAngle);
                _waitTimer = 0;
            }
        }

        private void SwitchScanDirection()
        {
            _isRotatingToMax = !_isRotatingToMax;
            _targetAngle = _isRotatingToMax ? maxAngle : minAngle;
            _waitTimer = 0;
        }

        private void ApplyRotation(float angle)
        {
            // Rotate around Z axis
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        #endregion

        #region Detection Math

        private bool CheckPlayerInSight()
        {
            if (!player) return false;

            // The "Forward" of the camera is its local Right (Red axis in Unity 2D usually)
            // Or local Down depending on your sprite. Assuming standard Right-facing sprite rotated down.
            Vector2 origin = transform.position;
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            float dstToPlayer = Vector2.Distance(transform.position, player.position);

            // 1. Distance Check
            if (dstToPlayer > viewDistance) return false;

            // 2. Angle Check
            // We use transform.right because we rotate the object itself
            float angleToPlayer = Vector2.Angle(transform.right, dirToPlayer);
            if (angleToPlayer > viewAngle / 2f) return false;

            // 3. Obstacle Check
            RaycastHit2D hit = Physics2D.Raycast(origin, dirToPlayer, dstToPlayer, obstacleLayer);
            if (hit.collider != null) return false;

            return true;
        }

        #endregion

        #region Visuals & Helper

        private void UpdateVisuals(bool isSpotted)
        {
            if (beamSprite)
            {
                float t = _suspicionTimer / timeToCatch;
                beamSprite.color = Color.Lerp(safeColor, alertColor, t);
            }
        }

        private void GameOver()
        {
            Debug.Log("GAME OVER - CAUGHT BY CAMERA");
            this.enabled = false;
        }

        private void OnDrawGizmos()
        {
            // Visualization for Editor
            Gizmos.color = _isAlerted ? Color.red : Color.green;
            
            // Draw view distance
            Gizmos.DrawWireSphere(transform.position, viewDistance);

            // Draw current FOV lines based on current rotation
            Vector3 facing = transform.right;
            Vector3 limitA = Quaternion.Euler(0, 0, viewAngle / 2) * facing;
            Vector3 limitB = Quaternion.Euler(0, 0, -viewAngle / 2) * facing;

            Gizmos.DrawLine(transform.position, transform.position + limitA * viewDistance);
            Gizmos.DrawLine(transform.position, transform.position + limitB * viewDistance);

            // Draw Scan Limits (Blue lines) to see where it will rotate
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Vector3 maxLimit = Quaternion.Euler(0, 0, maxAngle) * Vector3.right;
                Vector3 minLimit = Quaternion.Euler(0, 0, minAngle) * Vector3.right;
                Gizmos.DrawLine(transform.position, transform.position + maxLimit * (viewDistance * 0.5f));
                Gizmos.DrawLine(transform.position, transform.position + minLimit * (viewDistance * 0.5f));
            }
        }

        #endregion
    }
}
}
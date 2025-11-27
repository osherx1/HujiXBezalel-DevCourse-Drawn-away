using UnityEngine;

namespace Prototype1
{
    namespace Prototype1
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MuseumGuard : MonoBehaviour
    {
        #region Settings - Patrol
        [Header("Patrol Settings")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float patrolDistance = 5f; // Moves right and left from the start position
        [SerializeField] private float waitTimeAtEdge = 2f;
        #endregion

        #region Settings - Detection
        [Header("Detection Settings")]
        [SerializeField] private Transform flashlightOrigin; // The source of vision (eyes/flashlight)
        [SerializeField] private float viewDistance = 6f;
        [SerializeField] private float viewAngle = 50f;
        [SerializeField] private float timeToCatch = 1.5f; // Time required to fully detect the player
        [SerializeField] private LayerMask obstacleLayer; // Layer for walls to block vision
        
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer flashlightSprite;
        [SerializeField] private Color safeColor = new Color(1f, 1f, 0, 0.3f); // Yellowish
        [SerializeField] private Color alertColor = new Color(1f, 0, 0, 0.5f); // Reddish
        #endregion

        #region Private Variables
        private Rigidbody2D _rb;
        [SerializeField] private Transform player;
        private Vector2 _startPos;
        private Vector2 _targetPos;
        private float _waitTimer;
        private float _suspicionTimer;
        private bool _isMovingRight = true;
        private bool _isAlerted = false;
        #endregion

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _startPos = transform.position;
            
            // Set the first target point (moving to the right initially)
            _targetPos = _startPos + Vector2.right * patrolDistance;
            
            // Find the player by Tag (Crucial: Player object must have tag "Player")
            /*GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) _player = playerObj.transform;*/
        }

        private void Update()
        {
            if (player == null) return;

            // Check if player is currently visible
            bool canSeePlayer = CheckPlayerInSight();

            if (canSeePlayer)
            {
                HandleAlert();
            }
            else
            {
                HandlePatrol();
            }
            
            UpdateVisuals(canSeePlayer);
        }

        #region Core Logic

        private void HandleAlert()
        {
            // 1. Stop movement
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            _isAlerted = true;

            // 2. Increase suspicion meter
            _suspicionTimer += Time.deltaTime;

            // 3. Check for Game Over condition
            if (_suspicionTimer >= timeToCatch)
            {
                GameOver();
            }
        }

        private void HandlePatrol()
        {
            // If we were previously alerted, slowly decrease suspicion before moving again
            if (_suspicionTimer > 0)
            {
                _suspicionTimer -= Time.deltaTime;
                _rb.linearVelocity = Vector2.zero; // Wait until calm
                return;
            }
            
            _isAlerted = false;

            // Patrol Logic: Move towards target or wait
            float distanceToTarget = Vector2.Distance(transform.position, _targetPos);

            if (distanceToTarget < 0.2f)
            {
                // Reached destination - Wait
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                _waitTimer += Time.deltaTime;

                if (_waitTimer >= waitTimeAtEdge)
                {
                    SwitchDirection();
                }
            }
            else
            {
                // Move towards target
                Vector2 dir = (_targetPos - (Vector2)transform.position).normalized;
                _rb.linearVelocity = new Vector2(dir.x * moveSpeed, _rb.linearVelocity.y);
                
                // Reset wait timer while moving
                _waitTimer = 0;
            }
        }

        private void SwitchDirection()
        {
            _isMovingRight = !_isMovingRight;
            
            // Calculate new target position based on direction
            if (_isMovingRight)
                _targetPos = _startPos + Vector2.right * patrolDistance;
            else
                _targetPos = _startPos; // Or go left: _startPos - Vector2.right * patrolDistance

            FlipSprite();
            _waitTimer = 0;
        }

        #endregion

        #region Detection Math

        private bool CheckPlayerInSight()
        {
            if (!player) return false;

            Vector2 origin = flashlightOrigin != null ? flashlightOrigin.position : transform.position;
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            float dstToPlayer = Vector2.Distance(transform.position, player.position);

            // 1. Distance Check
            if (dstToPlayer > viewDistance) return false;

            // 2. Angle Check
            Vector2 facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            float angle = Vector2.Angle(facingDir, dirToPlayer);
            if (angle > viewAngle / 2f) return false;

            // 3. Obstacle Check (Raycast)
            // Raycast ensures we don't see through walls defined in 'obstacleLayer'
            RaycastHit2D hit = Physics2D.Raycast(origin, dirToPlayer, dstToPlayer, obstacleLayer);
            if (hit.collider != null) return false; // Hit a wall

            return true;
        }

        #endregion

        #region Visuals & Helper

        private void UpdateVisuals(bool isSpotted)
        {
            if (flashlightSprite)
            {
                // Interpolate color from Safe to Alert based on suspicion level
                float t = _suspicionTimer / timeToCatch;
                flashlightSprite.color = Color.Lerp(safeColor, alertColor, t);
            }
        }

        private void FlipSprite()
        {
            Vector3 scale = transform.localScale;
            scale.x = _isMovingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        private void GameOver()
        {
            Debug.Log("GAME OVER - CAUGHT BY GUARD");
            this.enabled = false; // Disable the guard logic
            
            // TODO: Trigger your game manager event here
            // Core.Managers.EventManager.Instance.InvokeEvent(EventNames.OnPlayerDeath, null);
        }

        private void OnDrawGizmos()
        {
            if (flashlightOrigin == null) return;
            
            Gizmos.color = _isAlerted ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(flashlightOrigin.position, viewDistance);

            // Draw cone lines in Editor
            Vector3 facing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Vector3 angleA = Quaternion.Euler(0, 0, viewAngle / 2) * facing;
            Vector3 angleB = Quaternion.Euler(0, 0, -viewAngle / 2) * facing;

            Gizmos.DrawLine(flashlightOrigin.position, flashlightOrigin.position + angleA * viewDistance);
            Gizmos.DrawLine(flashlightOrigin.position, flashlightOrigin.position + angleB * viewDistance);
        }

        #endregion
    }
}
}
using UnityEngine;
using UnityEngine.InputSystem;
using Drawing.Managers;

namespace Prototype1
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement (Toolkit)")]
        [Tooltip("When false the character snaps to max speed without easing.")]
        public bool useAcceleration = true;
        [SerializeField, Range(0f, 30f)] private float maxSpeed = 10f;
        [SerializeField, Range(0f, 100f)] private float maxAcceleration = 52f;
        [SerializeField, Range(0f, 100f)] private float maxDeceleration = 52f;
        [SerializeField, Range(0f, 100f)] private float maxTurnSpeed = 80f;
        [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 40f;
        [SerializeField, Range(0f, 100f)] private float maxAirDeceleration = 40f;
        [SerializeField, Range(0f, 100f)] private float maxAirTurnSpeed = 80f;
        [SerializeField, Range(0f, 5f)] private float friction = 0f;

        [Header("Jumping (Toolkit)")]
        [SerializeField, Range(1f, 8f)] private float jumpHeight = 5.5f;
        [SerializeField, Range(0.2f, 1.25f)] private float timeToJumpApex = 0.45f;
        [SerializeField, Range(0f, 5f)] private float upwardMovementMultiplier = 1f;
        [SerializeField, Range(1f, 10f)] private float downwardMovementMultiplier = 6f;
        [SerializeField, Range(0, 1)] private int maxAirJumps = 0;
        [Tooltip("Allow shorter jumps by releasing the button early.")]
        public bool variableJumpHeight = true;
        [SerializeField, Range(1f, 10f)] private float jumpCutOff = 4f;
        [SerializeField, Range(0f, 30f)] private float speedLimit = 25f;
        [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.15f;
        [SerializeField, Range(0f, 0.3f)] private float jumpBuffer = 0.15f;

        [Header("Grounding & Visuals")]
        public LayerMask groundLayer = 1;
        public SpriteRenderer spriteRenderer;

        [Header("Respawn")]
        [Tooltip("Optional transform used as the player's reset point.")]
        [SerializeField] private Transform respawnPoint;
        [Tooltip("Move the player to the respawn point right when the scene starts.")]
        [SerializeField] private bool snapToRespawnOnStart = true;

        [Header("Ground Check (Optional)")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.12f;

        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        private Rigidbody2D rb;
        private Collider2D boxCol;

        private bool isOnGround;
        private readonly Collider2D[] collisionResult = new Collider2D[1];
        private float directionX;
        private bool pressingKey;
        private Vector2 desiredVelocity;
        private Vector2 velocity;
        private float maxSpeedChange;
        private float acceleration;
        private float deceleration;
        private float turnSpeed;

        private bool desiredJump;
        private bool pressingJump;
        private float jumpBufferCounter;
        private float coyoteTimeCounter;
        private bool currentlyJumping;
        private int airJumpsRemaining;
        private float gravMultiplier = 1f;
        private float defaultGravityScale = 1f;
        private float jumpSpeed;

        private int facingDir = 1;
        private int lookDir = 1;
        private Vector3 fallbackSpawnPosition;
        private bool isGameFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            boxCol = GetComponent<Collider2D>();
            if (rb != null)
            {
                defaultGravityScale = Mathf.Max(rb.gravityScale, 0.0001f);
            }
            gravMultiplier = 1f;
            airJumpsRemaining = maxAirJumps;
            fallbackSpawnPosition = transform.position;
            if (respawnPoint != null)
            {
                fallbackSpawnPosition = respawnPoint.position;
            }
        }

        private void Start()
        {
            if (respawnPoint != null && snapToRespawnOnStart)
            {
                transform.position = respawnPoint.position;
            }
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null)
            {
                jumpAction.action.Enable();
                jumpAction.action.performed += OnJumpPerformed;
                jumpAction.action.canceled += OnJumpCanceled;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished += HandleGameFinished;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null)
            {
                jumpAction.action.performed -= OnJumpPerformed;
                jumpAction.action.canceled -= OnJumpCanceled;
                jumpAction.action.Disable();
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished -= HandleGameFinished;
            }
        }

        private void Update()
        {
            if (isGameFinished)
            {
                directionX = 0f;
                pressingKey = false;
                HandleResetShortcut();
                return;
            }

            CheckIsOnGround();

            directionX = ReadHorizontalInput();
            if (Mathf.Abs(directionX) > Mathf.Epsilon)
            {
                pressingKey = true;
                lookDir = directionX > 0f ? 1 : -1;
            }
            else
            {
                pressingKey = false;
                directionX = 0f;
            }

            if (facingDir != lookDir)
            {
                FlipSprite();
            }

            if (jumpAction == null)
            {
                PollFallbackJumpInput();
            }

            UpdateJumpAssistTimers(Time.deltaTime);

            HandleResetShortcut();
        }

        private void FixedUpdate()
        {
            if (isGameFinished)
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                return;
            }

            CheckIsOnGround();
            ApplyHorizontalMovement();
            ApplyJumpPhysics();
        }

        private void ApplyHorizontalMovement()
        {
            if (rb == null)
            {
                return;
            }

            desiredVelocity = new Vector2(directionX, 0f) * Mathf.Max(maxSpeed - friction, 0f);
            velocity = rb.linearVelocity;

            if (useAcceleration)
            {
                acceleration = isOnGround ? maxAcceleration : maxAirAcceleration;
                deceleration = isOnGround ? maxDeceleration : maxAirDeceleration;
                turnSpeed = isOnGround ? maxTurnSpeed : maxAirTurnSpeed;

                if (pressingKey)
                {
                    float inputSign = Mathf.Sign(directionX);
                    float velocitySign = Mathf.Sign(velocity.x);
                    bool turningAround = inputSign != 0f && inputSign != velocitySign;
                    maxSpeedChange = (turningAround ? turnSpeed : acceleration) * Time.fixedDeltaTime;
                }
                else
                {
                    maxSpeedChange = deceleration * Time.fixedDeltaTime;
                }

                velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
            }
            else
            {
                velocity.x = desiredVelocity.x;
            }

            rb.linearVelocity = new Vector2(velocity.x, rb.linearVelocity.y);
        }

        private void ApplyJumpPhysics()
        {
            if (rb == null)
            {
                return;
            }

            velocity = rb.linearVelocity;

            if (desiredJump)
            {
                if (TryConsumeJumpRequest())
                {
                    rb.linearVelocity = velocity;
                    return;
                }

                if (Mathf.Approximately(jumpBuffer, 0f))
                {
                    desiredJump = false;
                }
            }

            ApplyGravity();
        }

        private bool TryConsumeJumpRequest()
        {
            bool withinCoyoteWindow = !isOnGround && !currentlyJumping && coyoteTimeCounter > 0.03f && coyoteTimeCounter < coyoteTime;
            bool groundedJump = isOnGround || withinCoyoteWindow;

            if (!groundedJump && airJumpsRemaining <= 0)
            {
                return false;
            }

            desiredJump = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;

            if (groundedJump)
            {
                airJumpsRemaining = maxAirJumps;
            }
            else
            {
                airJumpsRemaining = Mathf.Max(airJumpsRemaining - 1, 0);
            }

            float gravityFactor = rb != null ? rb.gravityScale : 1f;
            jumpSpeed = Mathf.Sqrt(Mathf.Max(0f, -2f * Physics2D.gravity.y * gravityFactor * jumpHeight));

            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }
            else if (velocity.y < 0f)
            {
                jumpSpeed += Mathf.Abs(rb.linearVelocity.y);
            }

            velocity.y += jumpSpeed;
            currentlyJumping = true;

            return true;
        }

        private void ApplyGravity()
        {
            if (rb == null)
            {
                return;
            }

            float verticalVelocity = rb.linearVelocity.y;

            if (verticalVelocity > 0.01f)
            {
                if (isOnGround)
                {
                    gravMultiplier = defaultGravityScale;
                }
                else if (variableJumpHeight && pressingJump && currentlyJumping)
                {
                    gravMultiplier = upwardMovementMultiplier;
                }
                else if (variableJumpHeight)
                {
                    gravMultiplier = jumpCutOff;
                }
                else
                {
                    gravMultiplier = upwardMovementMultiplier;
                }
            }
            else if (verticalVelocity < -0.01f)
            {
                gravMultiplier = isOnGround ? defaultGravityScale : downwardMovementMultiplier;
            }
            else
            {
                if (isOnGround)
                {
                    currentlyJumping = false;
                    airJumpsRemaining = maxAirJumps;
                }

                gravMultiplier = defaultGravityScale;
            }

            UpdateGravityScale();

            Vector2 clampedVelocity = rb.linearVelocity;
            clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -Mathf.Abs(speedLimit), 100f);

            if (HasHitHead() && clampedVelocity.y > 0f)
            {
                clampedVelocity.y = 0f;
                currentlyJumping = false;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, clampedVelocity.y);
        }

        private void UpdateGravityScale()
        {
            if (rb == null)
            {
                return;
            }

            float apexTime = Mathf.Max(0.01f, timeToJumpApex);
            float baseGravity = (-2f * jumpHeight) / (apexTime * apexTime);
            float multiplier = Mathf.Max(gravMultiplier, 0f);
            rb.gravityScale = (baseGravity / Physics2D.gravity.y) * multiplier;
        }

        private void UpdateJumpAssistTimers(float deltaTime)
        {
            if (jumpBuffer > 0f && desiredJump)
            {
                jumpBufferCounter += deltaTime;
                if (jumpBufferCounter > jumpBuffer)
                {
                    desiredJump = false;
                    jumpBufferCounter = 0f;
                }
            }

            if (!currentlyJumping && !isOnGround)
            {
                coyoteTimeCounter += deltaTime;
            }
            else
            {
                coyoteTimeCounter = 0f;
            }

            if (isOnGround)
            {
                airJumpsRemaining = maxAirJumps;
            }
        }

        private float ReadHorizontalInput()
        {
            if (isGameFinished)
            {
                return 0f;
            }

            float axis = 0f;
            if (moveAction != null)
            {
                axis = moveAction.action.ReadValue<Vector2>().x;
            }
            else
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) axis -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) axis += 1f;
                }

                var gp = Gamepad.current;
                if (gp != null)
                {
                    axis += gp.leftStick.x.ReadValue();
                }
            }

            return Mathf.Clamp(axis, -1f, 1f);
        }

        private void PollFallbackJumpInput()
        {
            if (isGameFinished)
            {
                return;
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    desiredJump = true;
                    pressingJump = true;
                }

                if (kb.spaceKey.wasReleasedThisFrame)
                {
                    pressingJump = false;
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.buttonSouth.wasPressedThisFrame)
                {
                    desiredJump = true;
                    pressingJump = true;
                }

                if (gp.buttonSouth.wasReleasedThisFrame)
                {
                    pressingJump = false;
                }
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            if (isGameFinished)
            {
                return;
            }

            desiredJump = true;
            pressingJump = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            pressingJump = false;
        }

        private bool HasHitHead()
        {
            if (boxCol == null)
            {
                return false;
            }

            Vector2 origin = boxCol.bounds.center + (Vector3.up * (boxCol.bounds.extents.y * 0.75f));
            Vector2 size = new Vector2(boxCol.bounds.size.x * 0.6f, boxCol.bounds.extents.y * 0.45f);
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.NoFilter();
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(groundLayer);
            int collisionCount = Physics2D.OverlapBox(origin, size, 0f, contactFilter, collisionResult);
            return collisionCount != 0;
        }

        private void CheckIsOnGround()
        {
            if (boxCol != null)
            {
                Vector2 origin = boxCol.bounds.center + (Vector3.down * (boxCol.bounds.extents.y * 0.75f));
                Vector2 size = new Vector2(boxCol.bounds.size.x * 0.8f, boxCol.bounds.extents.y * 0.75f);
                ContactFilter2D contactFilter = new ContactFilter2D();
                contactFilter.NoFilter();
                contactFilter.useTriggers = false;
                contactFilter.SetLayerMask(groundLayer);
                int collisionCount = Physics2D.OverlapBox(origin, size, 0f, contactFilter, collisionResult);
                isOnGround = collisionCount != 0;
            }
            else if (groundCheck != null)
            {
                isOnGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            }
            else
            {
                isOnGround = false;
            }
        }

        private void FlipSprite()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = lookDir < 0;
            }
            else
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * lookDir;
                transform.localScale = scale;
            }

            facingDir = lookDir;
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D col = boxCol != null ? boxCol : GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = Color.yellow;
                Vector2 origin = col.bounds.center + (Vector3.down * (col.bounds.extents.y * 0.75f));
                Vector2 size = new Vector2(col.bounds.size.x * 0.8f, col.bounds.extents.y * 0.75f);
                Gizmos.DrawWireCube(origin, size);
            }
            else if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }

        private void HandleResetShortcut()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool ctrlHeld = (keyboard.leftCtrlKey != null && keyboard.leftCtrlKey.isPressed)
                             || (keyboard.rightCtrlKey != null && keyboard.rightCtrlKey.isPressed);

            if (ctrlHeld && keyboard.qKey != null && keyboard.qKey.wasPressedThisFrame)
            {
                ResetToRespawnPoint();
            }
        }

        public void ResetToRespawnPoint()
        {
            Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : fallbackSpawnPosition;
            transform.position = targetPosition;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            currentlyJumping = false;
            desiredJump = false;
            pressingJump = false;
            directionX = 0f;
            pressingKey = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            airJumpsRemaining = maxAirJumps;
        }

        private void HandleGameFinished()
        {
            isGameFinished = true;
            directionX = 0f;
            pressingKey = false;
            desiredJump = false;
            pressingJump = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }
}

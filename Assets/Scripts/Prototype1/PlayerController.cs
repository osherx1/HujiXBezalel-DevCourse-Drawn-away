using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototype1
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        public float moveSpeed = 10f;
        public float jumpSpeed = 5f;
        public float maxJumpLength = 0.5f;
        public float minJumpLength = 0.1f;
        public float jumpBufferInputLength = 0.2f;
        public float jumpForgiveLength = 0.06f;
        public LayerMask groundLayer = 1;
        public SpriteRenderer spriteRenderer;

        [Header("Fall Physics")]
        public bool enableFastFallGravity = true;
        public float fallGravityMultiplier = 2f;
        public float maxFallSpeed = 25f;

        [Header("Ground Check (Optional)")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.12f;

        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        private Rigidbody2D rb;
        private Collider2D boxCol;

        private int moveDir;
        private float movePower;
        private bool isOnGround;
        private readonly Collider2D[] collisionResult = new Collider2D[1];

        private bool wantToJump;
        private bool wantToStopJump;
        private bool isJumping;
        private bool jumpWasUsed;
        private float jumpTimer;
        private float jumpBufferedTimer;
        private float jumpForgiveTimer;
        private bool endJumpOnMin;
        private bool endNextJumpOnMin;

        private int facingDir = 1;
        private int lookDir = 1;

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
        }

        private void Update()
        {
            float moveDirFloat = ReadHorizontalInput();
            movePower = Mathf.Abs(moveDirFloat);

            if (moveDirFloat > Mathf.Epsilon)
            {
                moveDir = 1;
            }
            else if (moveDirFloat < -Mathf.Epsilon)
            {
                moveDir = -1;
            }
            else
            {
                moveDir = 0;
            }

            if (moveDir != 0)
            {
                lookDir = moveDir;
            }

            if (facingDir != lookDir)
            {
                FlipSprite();
            }

            // if no jump action is assigned we still need to poll inputs to support keyboard/gamepad defaults
            if (jumpAction == null)
            {
                PollFallbackJumpInput();
            }
        }

        private void FixedUpdate()
        {
            CheckIsOnGround();

            if (isOnGround)
            {
                jumpForgiveTimer = jumpForgiveLength;
            }
            else if (jumpForgiveTimer > 0f)
            {
                jumpForgiveTimer -= Time.fixedDeltaTime;
            }

            HandleJumpLogic();

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
                ApplyFallGravityBoost();
            }

            if (jumpBufferedTimer > 0f)
            {
                jumpBufferedTimer -= Time.fixedDeltaTime;
                if (jumpBufferedTimer < Mathf.Epsilon)
                {
                    endNextJumpOnMin = false;
                }
            }

            wantToJump = false;
            wantToStopJump = false;
        }

        private void ApplyFallGravityBoost()
        {
            if (!enableFastFallGravity || rb == null || rb.linearVelocity.y >= 0f)
            {
                return;
            }

            float multiplier = Mathf.Max(1f, fallGravityMultiplier);
            float extraGravity = Physics2D.gravity.y * (multiplier - 1f) * Time.fixedDeltaTime;
            float newY = rb.linearVelocity.y + extraGravity;
            if (newY < -Mathf.Abs(maxFallSpeed))
            {
                newY = -Mathf.Abs(maxFallSpeed);
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
        }

        private float ReadHorizontalInput()
        {
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
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) wantToJump = true;
                if (kb.spaceKey.wasReleasedThisFrame) wantToStopJump = true;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.buttonSouth.wasPressedThisFrame) wantToJump = true;
                if (gp.buttonSouth.wasReleasedThisFrame) wantToStopJump = true;
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            wantToJump = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            wantToStopJump = true;
        }

        private void HandleJumpLogic()
        {
            if (rb == null)
            {
                return;
            }

            if (isOnGround && jumpWasUsed)
            {
                jumpWasUsed = false;
            }

            bool canJump = (isOnGround || jumpForgiveTimer > Mathf.Epsilon)
                           && (wantToJump || jumpBufferedTimer > Mathf.Epsilon)
                           && !isJumping && !jumpWasUsed;

            if (canJump)
            {
                if (endNextJumpOnMin)
                {
                    endNextJumpOnMin = false;
                    endJumpOnMin = true;
                }

                isJumping = true;
                jumpWasUsed = true;
                jumpTimer = 0f;
                jumpBufferedTimer = 0f;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            }
            else if (wantToJump)
            {
                jumpBufferedTimer = jumpBufferInputLength;
            }

            if (wantToStopJump && isJumping)
            {
                if (jumpTimer < minJumpLength)
                {
                    endJumpOnMin = true;
                }
                else
                {
                    StopJump();
                }
            }

            if (isJumping)
            {
                jumpTimer += Time.fixedDeltaTime;
                if (jumpTimer >= maxJumpLength || (jumpTimer >= minJumpLength && endJumpOnMin) || HasHitHead())
                {
                    endJumpOnMin = false;
                    StopJump();
                }
                else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
                }
            }
        }

        private void StopJump()
        {
            if (rb == null)
            {
                return;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            isJumping = false;
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
    }
}

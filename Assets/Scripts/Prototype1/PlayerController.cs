using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototype1
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpForce = 12f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.12f;
        public LayerMask groundLayer;

        [Header("Input (New Input System)")]
        [SerializeField] private InputActionReference moveAction; // expected Vector2 (x = horizontal)
        [SerializeField] private InputActionReference jumpAction; // expected Button

        private Rigidbody2D _rb;
        private bool _isGrounded;
        private float _horizontal;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null)
            {
                jumpAction.action.Enable();
                jumpAction.action.performed += OnJumpPerformed;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null)
            {
                jumpAction.action.performed -= OnJumpPerformed;
                jumpAction.action.Disable();
            }
        }

        private void Update()
        {
            if (moveAction != null)
            {
                var v = moveAction.action.ReadValue<Vector2>();
                _horizontal = v.x;
            }
            else
            {
                // fallback to new Input System direct device read if no actions assigned
                _horizontal = 0f;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) _horizontal -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) _horizontal += 1f;
                }

                var gp = Gamepad.current;
                if (gp != null) _horizontal = Mathf.Clamp(_horizontal + gp.leftStick.x.ReadValue(), -1f, 1f);
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log("Jump!");
            if (_isGrounded && _rb != null)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            }
        }

        private void FixedUpdate()
        {
            if (groundCheck != null)
            {
                _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            }
            else
            {
                _isGrounded = false;
            }

            if (_rb != null)
            {
                _rb.linearVelocity = new Vector2(_horizontal * moveSpeed, _rb.linearVelocity.y);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

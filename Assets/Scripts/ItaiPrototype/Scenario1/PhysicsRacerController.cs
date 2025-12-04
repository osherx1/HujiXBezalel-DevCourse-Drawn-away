using ItaiPrototype.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

// REQUIRED for New Input System

namespace ItaiPrototype.Scenario1
{
    public class PhysicsRacerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float groundTorque = 100f;
        [SerializeField] private float airTorque = 50f;
        [SerializeField] private float jumpForce = 15f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDist = 0.1f;
        
        [Header("Limits")]
        [SerializeField] private float maxRotationSpeed = 400f; // Max degrees per second

        private Rigidbody2D _targetRb;
        private Collider2D _targetCollider;
        private bool _isGrounded;

        private void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.storedDrawing == null) return;
            GameObject drawing = GameManager.Instance.storedDrawing;
            _targetRb = drawing.GetComponent<Rigidbody2D>();
            _targetCollider = drawing.GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (!_targetRb) return;
        
            // Safety check: Ensure a keyboard is connected
            if (Keyboard.current == null) return;

            // --- JUMP (Spacebar) ---
            // Old: Input.GetKeyDown(KeyCode.Space)
            if (Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded)
            {
                _targetRb.AddForce(Vector2.up * (jumpForce * _targetRb.mass), ForceMode2D.Impulse);
            }
        }

        private void FixedUpdate()
        {
            if (!_targetRb) return;

            CheckGrounded();

            // Safety check
            if (Keyboard.current == null) return;

            // --- ROTATION (A/D or Arrows) ---
            // Old: Input.GetAxis("Horizontal")
            float input = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                input = -1f;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                input = 1f;
            }

            if (!(Mathf.Abs(input) > 0.1f)) return;
            
            // Angular Velocity is negative when spinning Right, positive when spinning Left.
            // Case 1: Trying to spin LEFT (Negative Input) but already spinning too fast to the left
            if (input < 0 && _targetRb.angularVelocity > maxRotationSpeed) return;

            // Case 2: Trying to spin RIGHT (Positive Input) but already spinning too fast to the right
            if (input > 0 && _targetRb.angularVelocity < -maxRotationSpeed) return;
            // ------------------------------

            float torquePower = _isGrounded ? groundTorque : airTorque;
            _targetRb.AddTorque(-input * torquePower * _targetRb.mass);
        }

        private void CheckGrounded()
        {
            if (!_targetCollider) return;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayer);
            filter.useTriggers = false;

            RaycastHit2D[] results = new RaycastHit2D[1];
            int hitCount = _targetCollider.Cast(Vector2.down, filter, results, groundCheckDist);

            _isGrounded = hitCount > 0;
        }
    }
}
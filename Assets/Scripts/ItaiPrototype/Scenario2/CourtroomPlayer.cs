using UnityEngine;
using UnityEngine.InputSystem;

namespace ItaiPrototype.Scenario2
{
    public class CourtroomPlayer : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform groundCheck; // A tiny empty object at feet

        private Rigidbody2D rb;
        private bool isGrounded;
        private Vector2 moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            // CRITICAL: Prevent the heavy weapon from flipping the player
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Move Input
            float x = 0;
            if (Keyboard.current.aKey.isPressed) x = -1;
            if (Keyboard.current.dKey.isPressed) x = 1;
            moveInput = new Vector2(x, 0);

            // Jump Input
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        private void FixedUpdate()
        {
            // Move the player horizontally
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }
}
using ItaiPrototype.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItaiPrototype.Scenario3
{
    public class ShovelGrip : MonoBehaviour
    {
        [Header("Control Settings")]
        [SerializeField] private float moveSpeed = 15f;      // How fast the hand follows mouse
        [SerializeField] private float rotationPower = 200f; // How fast it spins
        [SerializeField] private Transform handPosition;     // The invisible anchor

        private Rigidbody2D weaponRb;
        private SpringJoint2D spring;

        void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.storedDrawing == null) return;

            GameObject drawing = GameManager.Instance.storedDrawing;
        
            // 1. Setup Drawing
            drawing.transform.SetParent(null);
            drawing.transform.rotation = Quaternion.identity;
            drawing.SetActive(true);
            drawing.tag = "Player"; 

            weaponRb = drawing.GetComponent<Rigidbody2D>();
            // Make it heavy enough to push dirt, but not impossible to lift
            weaponRb.mass = Mathf.Clamp(weaponRb.mass, 5f, 15f); 
            weaponRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 2. Find Handle (Top-most point for a shovel)
            float maxWorldY = float.MinValue;
            Collider2D[] cols = drawing.GetComponentsInChildren<Collider2D>();
        
            if (cols.Length > 0)
            {
                foreach (var col in cols) 
                {
                    if (col.bounds.max.y > maxWorldY) maxWorldY = col.bounds.max.y;
                }
            }
            else
            {
                maxWorldY = drawing.transform.position.y;
            }
        
            // Calculate offset to grab the Top
            Vector3 topHandlePos = new Vector3(drawing.transform.position.x, maxWorldY, 0);
            Vector3 offset = topHandlePos - drawing.transform.position;

            // 3. Move Drawing to Hand
            drawing.transform.position = handPosition.position - offset;

            // 4. Spring Joint (The Grip)
            // We use a Spring instead of a Hinge so you can "push" against the dirt resistance
            spring = gameObject.AddComponent<SpringJoint2D>();
            spring.connectedBody = weaponRb;
            spring.autoConfigureConnectedAnchor = false;
            spring.anchor = Vector2.zero; 
            spring.connectedAnchor = drawing.transform.InverseTransformPoint(drawing.transform.position + offset);
        
            spring.frequency = 8f;  // Stiff spring
            spring.dampingRatio = 0.8f; // Less bouncy
        }

        void FixedUpdate()
        {
            if (weaponRb == null || Mouse.current == null) return;
            if (Keyboard.current == null) return;

            // 1. POSITION: Move the Hand to the Mouse
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0; // Ensure we stay in 2D plane
        
            // Smoothly move the anchor point towards mouse
            Vector3 newPos = Vector3.Lerp(transform.position, mousePos, moveSpeed * Time.fixedDeltaTime);
            transform.position = newPos;

            // 2. ROTATION: Use A/D Keys (New Input System)
            float rotateInput = 0f;
            if (Keyboard.current.aKey.isPressed) rotateInput = 1f;  // Rotate Left (CCW)
            if (Keyboard.current.dKey.isPressed) rotateInput = -1f; // Rotate Right (CW)

            if (rotateInput != 0)
            {
                weaponRb.AddTorque(rotateInput * rotationPower * Time.fixedDeltaTime, ForceMode2D.Impulse);
            }
        
            // Stabilizer: Add a little drag if no input is pressed so it doesn't spin forever
            else
            {
                weaponRb.angularVelocity = Mathf.Lerp(weaponRb.angularVelocity, 0, Time.fixedDeltaTime * 2f);
            }
        }
    }
}
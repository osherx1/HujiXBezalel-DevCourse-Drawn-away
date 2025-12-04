using ItaiPrototype.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItaiPrototype.Scenario2
{
    public class WeaponGrip : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private float swingPower = 800f;
        [SerializeField] private Transform handPosition;

        private Rigidbody2D weaponRb;
        private Rigidbody2D playerRb;
        private HingeJoint2D joint;
    
        // Debug
        private Vector3 debugHandlePos;

        void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.storedDrawing == null) return;
        
            GameObject drawing = GameManager.Instance.storedDrawing;
        
            // 1. DETACH & RESET
            // We detach the drawing from the GameManager to ensure clean world coordinates
            drawing.transform.SetParent(null); 
            // We reset rotation to 0 to ensure "Left" is actually "Left" during calculation
            drawing.transform.rotation = Quaternion.identity; 
            drawing.SetActive(true);

            playerRb = GetComponent<Rigidbody2D>();
            weaponRb = drawing.GetComponent<Rigidbody2D>();

            // 2. PREVENT FALLING (Physics Settings)
            // Set collision mode to Continuous so heavy objects don't fall through floor
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            weaponRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Mass Clamp: Don't let the player become a black hole
            float desiredMass = weaponRb.mass * 2;
            playerRb.mass = Mathf.Clamp(desiredMass, 1f, 20f); // Cap max mass at 20

            // 3. IGNORE COLLISION (Player vs Weapon)
            Collider2D[] pCols = GetComponentsInChildren<Collider2D>();
            Collider2D[] wCols = drawing.GetComponentsInChildren<Collider2D>();
            foreach (var p in pCols) foreach (var w in wCols) Physics2D.IgnoreCollision(p, w);

            // 4. FIND HANDLE (Bounds Method)
            // Since we reset rotation, bounds.min.x is GUARANTEED to be the left-most edge
            float minWorldX = float.MaxValue;
            Collider2D[] allColliders = drawing.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D col in allColliders)
            {
                if (col.bounds.min.x < minWorldX)
                {
                    minWorldX = col.bounds.min.x;
                }
            }

            // If no colliders found, fallback to center
            if (minWorldX == float.MaxValue) minWorldX = drawing.transform.position.x;

            // The handle is at {LeftMostX, CenterY, CenterZ}
            Vector3 handleWorldPos = new Vector3(minWorldX, drawing.transform.position.y, drawing.transform.position.z);
            debugHandlePos = handleWorldPos;

            // 5. MOVE DRAWING
            // Offset = Handle - Center
            Vector3 offset = handleWorldPos - drawing.transform.position;
        
            // Teleport drawing so Handle is at Hand
            drawing.transform.position = handPosition.position - offset;
            drawing.tag = "Player";

            // 6. JOINT
            joint = gameObject.AddComponent<HingeJoint2D>();
            joint.connectedBody = weaponRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = handPosition.localPosition;
        
            // Calculate the anchor point in the Weapon's LOCAL space
            joint.connectedAnchor = drawing.transform.InverseTransformPoint(drawing.transform.position + offset);

            weaponRb.angularDamping = 1f; 
        }

        void FixedUpdate()
        {
            if (weaponRb == null || Mouse.current == null) return;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 direction = (Vector2)mousePos - (Vector2)handPosition.position;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
            float angleDiff = Mathf.DeltaAngle(weaponRb.rotation, targetAngle);
            weaponRb.AddTorque(angleDiff * swingPower * Time.fixedDeltaTime);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(debugHandlePos, 0.2f);
        }
    }
}
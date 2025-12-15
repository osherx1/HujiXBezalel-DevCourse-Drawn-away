using UnityEngine;

namespace Physics.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TrapdoorController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float closeAngle = 0f;
        [SerializeField] private float moveSpeed = 2f;

        private Rigidbody2D _rb;
        private float _targetAngle;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Ensure the body type is Kinematic so script controls it, not gravity
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _targetAngle = closeAngle;
        }

        private void FixedUpdate()
        {
            // Smoothly interpolate current rotation to target rotation
            float currentAngle = _rb.rotation;
            float newAngle = Mathf.LerpAngle(currentAngle, _targetAngle, moveSpeed * Time.fixedDeltaTime);
        
            _rb.MoveRotation(newAngle);
        }

        public void Open()
        {
            _targetAngle = openAngle;
        }

        public void Close()
        {
            _targetAngle = closeAngle;
        }
    }
}
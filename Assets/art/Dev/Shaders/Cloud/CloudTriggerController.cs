using UnityEngine;
// Keeping your original using

namespace art.Dev.Shaders.Cloud
{
    // 1. Define the Enum for clear direction selection
    public enum CloudMoveDirection
    {
        Right,
        Left
    }

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CloudTriggerController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CloudMoveDirection moveDirection = CloudMoveDirection.Right; // Enum instead of Vector3
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float windIntensity = 1.0f;
        
        [Header("References")]
        [SerializeField] private Transform startPositionTransform;

        private Vector3 _startPoint;
        private _Local.Shaders.Cloud.CloudShaderAdapter _shaderAdapter;

        // 2. Logic to convert Enum to Vector3
        private Vector3 CurrentDirectionVector
        {
            get
            {
                return moveDirection == CloudMoveDirection.Right ? Vector3.right : Vector3.left;
            }
        }

        private void Awake()
        {
            // Initialize starting point Y from current position
            _startPoint = transform.position;

            // Initialize Shader Adapter
            var renderer = GetComponent<SpriteRenderer>();
            Material instanceMat = new Material(renderer.sharedMaterial);
            renderer.material = instanceMat;
            
            // Assuming CloudShaderAdapter is defined in your project based on previous context
            _shaderAdapter = new _Local.Shaders.Cloud.CloudShaderAdapter(instanceMat);
        }

        private void Start()
        {
            _shaderAdapter.UpdateWindEffect(windIntensity);
        }

        private void Update()
        {
            // Move the cloud using the Enum-based direction
            transform.position = CalculateMovement(
                transform.position, 
                CurrentDirectionVector, 
                Time.deltaTime
            );
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("CloudEndPoint"))
            {
                ResetToStart();
            }
        }

        private void ResetToStart()
        {
            // Reset to the X of the start transform, but keep the original Y
            transform.position = new Vector3(startPositionTransform.position.x, _startPoint.y, 0f);
        }

        // Pure logic method
        public Vector3 CalculateMovement(Vector3 currentPosition, Vector3 direction, float deltaTime)
        {
            return currentPosition + (direction * moveSpeed * deltaTime);
        }
    }
}
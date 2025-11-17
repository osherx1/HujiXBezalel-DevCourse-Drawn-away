using UnityEngine;
using UnityEngine.InputSystem;

namespace Drawing
{
    [RequireComponent(typeof(AudioSource))]
    public class CursorController : MonoBehaviour
    {
        // --- Sound Settings ---
        [SerializeField] private float minDrawingSpeed = 0.1f;
        [SerializeField] private float maxDrawingSpeed = 10f;
    
        [SerializeField] private float minPitch = 0.8f;
        [SerializeField] private float maxPitch = 1.3f;
    
        [SerializeField] private float maxVolume = 0.5f;

        // --- Mouse Following ---
        [SerializeField] private float zOffsetFromCamera = 10f;
        [SerializeField] private float smoothingFactor = 5f;
        private AudioSource _audioSource;
        private Vector3 _lastPosition;
        private Camera _camera;
        private float _currentSmoothedSpeed;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource.clip == null)
            {
                Debug.LogWarning("DrawingSoundController: AudioSource is missing an AudioClip.", this);
            }

            _audioSource.loop = true;
            _audioSource.playOnAwake = true;
            _audioSource.volume = 0; 
            _audioSource.pitch = minPitch;
            _audioSource.Play();
        }

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("DrawingSoundController: No main camera found. Mouse following will not work.", this);
                enabled = false;
                return;
            }

            _lastPosition = GetMouseWorldPosition();
            transform.position = _lastPosition;
        }

        private void Update()
        {
            if (_camera == null || Mouse.current == null) return; // Safety check for mouse

            // 1. Get current mouse position in world space
            Vector3 currentMouseWorldPos = GetMouseWorldPosition();

            float normalizedSpeed = 0f;


            // 2. Check if the left mouse button is held down (using new Input System)
            if (Mouse.current.leftButton.isPressed)
            {
                float distance = Vector3.Distance(currentMouseWorldPos, _lastPosition);
                float currentSpeed = distance / Time.deltaTime;

                normalizedSpeed = Mathf.InverseLerp(minDrawingSpeed, maxDrawingSpeed, currentSpeed);
                normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

                _currentSmoothedSpeed = Mathf.Lerp(
                    _currentSmoothedSpeed, 
                    normalizedSpeed, 
                    smoothingFactor * Time.deltaTime
                );

                _audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, normalizedSpeed);
                _audioSource.volume = normalizedSpeed * maxVolume;
            }
            else
            {
                // 3. If mouse button is up, silence the audio
                _audioSource.volume = 0;
            }

            // 4. Update the object's position to follow the mouse
            transform.position = currentMouseWorldPos;

            // 5. Save the current position for the next frame's speed calculation
            _lastPosition = currentMouseWorldPos;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Mouse.current == null)
            {
                return transform.position; // Return last known position if mouse is not available
            }

            // 3. Get mouse position from new Input System
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        
            mouseScreenPos.z = zOffsetFromCamera; 
            return _camera.ScreenToWorldPoint(mouseScreenPos);
        }
    }
}
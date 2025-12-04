using Drawing;
using Drawing.Data;
using Drawing.Managers;
using ItaiPrototype.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ItaiPrototype.DrawingScreen
{
    [RequireComponent(typeof(AudioSource))]
    public class ItaiCursorController : MonoSingleton<ItaiCursorController>
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
        private bool _isEraserMode;



        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("DrawingSoundController: No main camera found. Mouse following will not work.", this);
                enabled = false;
                return;
            }
            var config = DrawingConfigController.Instance.currentSettings;
            if (config == null)
            {
                Debug.LogWarning("[Cursor Controller] - Current drawing configuration is null.", this);
                return;
            }

            Debug.Log("[Cursor Controller] - Drawing sound set to: " + config.drawSound, this);
            ItaiAudioManager.Instance.SetBackgroundMusic(config.drawSound); 
            
            _audioSource = ItaiAudioManager.Instance.GetBackgroundMusicAudioSource();
            if (_audioSource.clip == null)
            {
                Debug.LogWarning("DrawingSoundController: AudioSource is missing an AudioClip.", this);
            }

            _audioSource.loop = true;
            _audioSource.volume = 0; 
            _audioSource.pitch = minPitch;
            _audioSource.Play();

            _lastPosition = GetMouseWorldPosition();
            transform.position = _lastPosition;
            UpdateCursorVisibility();
        }

        private void Update()
        {
            if (_camera == null || Mouse.current == null)
            {
                UpdateCursorVisibility();
                return; // Safety check for mouse
            }

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

            UpdateCursorVisibility();
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

        private void OnEnable()
        {
            //listen to event of button pressed to change the drawing sound
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnConfigButtonSelected += HandleButtonPressed;
                EventManager.Instance.OnEraserActive += HandleEraserActive;
                EventManager.Instance.OnEraserInactive += HandleEraserInactive;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            
        }

        private void HandleEraserActive()
        {
            var config = DrawingConfigController.Instance.currentSettings;
            //TODO maybe need to change to eraser sound later
            ItaiAudioManager.Instance.SetBackgroundMusic(GameSoundsSo.AudioType.None);
            _isEraserMode = true;
            UpdateCursorVisibility();
        }

        private void HandleEraserInactive()
        {
            _isEraserMode = false;
            HandleButtonPressed(null);
            UpdateCursorVisibility();
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnConfigButtonSelected -= HandleButtonPressed;
                EventManager.Instance.OnEraserActive -= HandleEraserActive;
                EventManager.Instance.OnEraserInactive -= HandleEraserInactive;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            _isEraserMode = false;
            Cursor.visible = true;

        }

        private void HandleButtonPressed(object obj)
        {
            var config = DrawingConfigController.Instance.currentSettings;
            if (config != null)
            {
                ItaiAudioManager.Instance.SetBackgroundMusic(config.drawSound);
            }
            
            UpdateCursorVisibility();
        }

        private void UpdateCursorVisibility()
        {
            bool shouldShowCursor = true;

            if (_isEraserMode)
            {
                shouldShowCursor = IsPointerOverUI();
            }

            if (Cursor.visible != shouldShowCursor)
            {
                Cursor.visible = shouldShowCursor;
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "DrawingScreen")
            {
                // We are back! 
                EnableCursor();
            }
            else
            {
                // We are in a game level (Situation 1, etc.)
                DisableCursor();
            }
        }

        private void EnableCursor()
        {
            // Re-enable this script (so Update() runs again)
            enabled = true;
        
            // Or if the sprite is on this object:
            if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = true;
        }

        private void DisableCursor()
        {
            // Stop this script from processing input
            this.enabled = false;
        
            // Hide the visuals
            if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
        }
    }
}
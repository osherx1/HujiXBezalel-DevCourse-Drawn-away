using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

using Unity.Cinemachine; // Required for the fix

namespace Drawing.LineControl
{
    [RequireComponent(typeof(AudioSource))]
    public class CursorController : MonoBehaviour
    {
        [Header("Debug Settings")] 
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Sound Settings")] 
        [SerializeField] private float minDrawingSpeed = 0.1f;
        [SerializeField] private float maxDrawingSpeed = 10f;
        [SerializeField] private float minPitch = 0.8f;
        [SerializeField] private float maxPitch = 1f;
        [SerializeField] private float maxVolume = 0.5f;
        [SerializeField] private float soundSmoothingFactor = 15f;

        [Header("Mouse Following")] 
        [SerializeField] private float zOffsetFromCamera = 10f;

        private AudioSource _localAudioSource;
        private Camera _mainCamera;
        
        private Vector3 _lastFramePosition;
        private float _currentSmoothedSpeed;

        private bool _isEraserMode;
        private bool _isPaused;
        private bool _lastCursorVisibleState = true;
        private Vector3 _targetPosition;

        private void Awake()
        {
            InitializeAudioSource();
        }

        private void Start()
        {
            _lastCursorVisibleState = true;
            Cursor.visible = true;
            InitializeCamera();
            UpdateAudioClipFromConfig();
            
            if (_mainCamera != null)
            {
                Vector3 startPos = GetMouseWorldPosition();
                transform.position = startPos;
                _lastFramePosition = startPos;
            }

            if (_localAudioSource.clip != null)
            { 
                _localAudioSource.Play();
            }
        }

        private void OnEnable()
        {
            RegisterEvents();
            
            // SENIOR FIX: Subscribe to the Cinemachine update pipeline
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCinemachineCameraUpdated);
        }

        private void OnDisable()
        {
            UnregisterEvents();
            
            // SENIOR FIX: Clean up the event listener
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCinemachineCameraUpdated);
            
            Cursor.visible = true;
            if (_localAudioSource != null) _localAudioSource.Stop();
        }

        // We use Update purely for non-positional logic (Cursor Visibility)
        // Positional logic is now handled in OnCinemachineCameraUpdated
        private void Update()
        {
            UpdateSystemCursorVisibility();
        }

        /// <summary>
        /// This method runs immediately after Cinemachine has finished moving the camera.
        /// This guarantees the Camera Transform is final for the current frame before we calculate screen-to-world.
        /// </summary>
        private void OnCinemachineCameraUpdated(CinemachineBrain brain)
        {
            if (!IsSystemReady()) return;

            // Ensure we only update if the brain that just updated is controlling our Main Camera
            if (brain.OutputCamera != _mainCamera) return;

            HandlePosition();
            HandleAudio();
        }

        private void InitializeAudioSource()
        {
            _localAudioSource = GetComponent<AudioSource>();
            if(_localAudioSource == null)
            {
                if (enableDebugLogs) Debug.LogError("[CursorController] No AudioSource component found! Disabling script.");
                return;
            }

            if (!_localAudioSource.isActiveAndEnabled)
            {
                _localAudioSource.enabled = true;
            }
            
            _localAudioSource.loop = true;
            _localAudioSource.playOnAwake = false;
            _localAudioSource.spatialBlend = 0f; 
            _localAudioSource.volume = 0f;
        }

        private void InitializeCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                if (enableDebugLogs) Debug.LogError("[CursorController] No Main Camera found. Disabling script.");
                enabled = false;
            }
        }

        private bool IsSystemReady()
        {
            return _mainCamera != null && Mouse.current != null && !_isPaused;
        }

        private void HandlePosition()
        {
            // Now safe to call because Camera.transform is finalized for the frame
            _targetPosition = GetMouseWorldPosition();
            
            transform.position = _targetPosition;
        }

        private void HandleAudio()
        {
            // Calculate speed based on distance moved in this frame
            float distance = Vector3.Distance(_targetPosition, _lastFramePosition);
            float instantSpeed = distance / Time.deltaTime;

            _lastFramePosition = _targetPosition;

            bool isDrawing = EvaluateIsDrawing();

            if (isDrawing)
            {
                ApplyDrawingAudio(instantSpeed);
            }
            else
            {
                FadeOutAudio();
            }
        }

        private bool EvaluateIsDrawing()
        {
            bool isLeftClick = Mouse.current.leftButton.isPressed;
            bool hasClip = _localAudioSource.clip != null;
            
            return isLeftClick && !_isEraserMode && hasClip && !IsPointerOverUI();
        }

        private void ApplyDrawingAudio(float instantSpeed)
        {
            float normalizedSpeed = Mathf.InverseLerp(minDrawingSpeed, maxDrawingSpeed, instantSpeed);
            _currentSmoothedSpeed = Mathf.Lerp(_currentSmoothedSpeed, normalizedSpeed, soundSmoothingFactor * Time.deltaTime);

            float globalVolMult = (AudioManager.Instance != null) ? AudioManager.Instance.GetVolumeMult() : 1f;

            _localAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, _currentSmoothedSpeed);
            _localAudioSource.volume = _currentSmoothedSpeed * maxVolume * globalVolMult;
        }

        private void FadeOutAudio()
        {
            _localAudioSource.volume = Mathf.Lerp(_localAudioSource.volume, 0f, 10f * Time.deltaTime);
            _currentSmoothedSpeed = 0f;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Mouse.current == null) return transform.position;

            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = zOffsetFromCamera;
            return _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }

        private void UpdateSystemCursorVisibility()
        {
            bool shouldShowSystemCursor = !(_isEraserMode && !IsPointerOverUI());

            if (_lastCursorVisibleState != shouldShowSystemCursor)
            {
                Cursor.visible = shouldShowSystemCursor;
                _lastCursorVisibleState = shouldShowSystemCursor;
            }
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        #region Event Handlers

        private void RegisterEvents()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnConfigButtonSelected += HandleConfigChanged;
            EventManager.Instance.OnEraserActive += HandleEraserActive;
            EventManager.Instance.OnEraserInactive += HandleEraserInactive;
            EventManager.Instance.OnGamePausedChanged += HandleGamePaused;
        }

        private void UnregisterEvents()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnConfigButtonSelected -= HandleConfigChanged;
            EventManager.Instance.OnEraserActive -= HandleEraserActive;
            EventManager.Instance.OnEraserInactive -= HandleEraserInactive;
            EventManager.Instance.OnGamePausedChanged -= HandleGamePaused;
        }

        private void HandleGamePaused(bool isPaused)
        {
            _isPaused = isPaused;
            if (_isPaused) _localAudioSource.volume = 0;
        }

        private void HandleConfigChanged(object obj)
        {
            HandleEraserInactive();
        }

        private void HandleEraserActive()
        {
            _isEraserMode = true;
            _localAudioSource.Stop();
        }

        private void HandleEraserInactive()
        {
            _isEraserMode = false;
            UpdateAudioClipFromConfig();
            if (_localAudioSource.clip != null) _localAudioSource.Play();
        }

        private void UpdateAudioClipFromConfig()
        {
            if (DrawingConfigController.Instance == null || AudioManager.Instance == null) return;

            var config = DrawingConfigController.Instance.currentSettings;
            if (config != null)
            {
                _localAudioSource.clip = AudioManager.Instance.GetClip(config.drawSound);
            }
        }

        #endregion
    }
}

/*using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Drawing.LineControl
{
    [RequireComponent(typeof(AudioSource))]
    public class CursorController : MonoBehaviour
    {
        [Header("Debug Settings")] 
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Sound Settings")] 
        [SerializeField] private float minDrawingSpeed = 0.1f;
        [SerializeField] private float maxDrawingSpeed = 10f;
        [SerializeField] private float minPitch = 0.8f;
        [SerializeField] private float maxPitch = 1f;
        [SerializeField] private float maxVolume = 0.5f;
        [SerializeField] private float soundSmoothingFactor = 15f; // Renamed for clarity

        [Header("Mouse Following")] 
        [SerializeField] private float zOffsetFromCamera = 10f;
        // Removed position smoothing factor to fix jitter/lag

        private AudioSource _localAudioSource;
        private Camera _mainCamera;
        
        private Vector3 _lastFramePosition;
        private float _currentSmoothedSpeed;

        private bool _isEraserMode;
        private bool _isPaused;
        private bool _lastCursorVisibleState = true;
        private Vector3 _targetPosition;

        private void Awake()
        {
            InitializeAudioSource();
        }

        private void Start()
        {
            InitializeCamera();
            UpdateAudioClipFromConfig();
            
            // Initial position setup
            if (_mainCamera != null)
            {
                Vector3 startPos = GetMouseWorldPosition();
                transform.position = startPos;
                _lastFramePosition = startPos;
            }

            if (_localAudioSource.clip != null)
            {
                _localAudioSource.Play();
            }
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
            
            Cursor.visible = true;
            if (_localAudioSource != null) _localAudioSource.Stop();
        }

        // Changed from Update to LateUpdate to sync with Cinemachine camera movement
        private void LateUpdate()
        {
            if (!IsSystemReady())
            {
                UpdateSystemCursorVisibility();
                return;
            }

            HandlePosition();
            HandleAudio();
            UpdateSystemCursorVisibility();
        }

        private void InitializeAudioSource()
        {
            _localAudioSource = GetComponent<AudioSource>();
            _localAudioSource.loop = true;
            _localAudioSource.playOnAwake = false;
            _localAudioSource.spatialBlend = 0f; // 2D Sound
            _localAudioSource.volume = 0f;
        }

        private void InitializeCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                if (enableDebugLogs) Debug.LogError("[CursorController] No Main Camera found. Disabling script.");
                enabled = false;
            }
        }

        private bool IsSystemReady()
        {
            return _mainCamera != null && Mouse.current != null && !_isPaused;
        }

        private void HandlePosition()
        {
            _targetPosition = GetMouseWorldPosition();
            
            // Direct assignment avoids jitter caused by Lerp fighting with Camera movement
            transform.position = _targetPosition;
        }

        private void HandleAudio()
        {
            // Calculate speed based on distance moved in this frame
            float distance = Vector3.Distance(_targetPosition, _lastFramePosition);
            float instantSpeed = distance / Time.deltaTime;

            // Store position for next frame calculation
            _lastFramePosition = _targetPosition;

            bool isDrawing = EvaluateIsDrawing();

            if (isDrawing)
            {
                ApplyDrawingAudio(instantSpeed);
            }
            else
            {
                FadeOutAudio();
            }
        }

        private bool EvaluateIsDrawing()
        {
            bool isLeftClick = Mouse.current.leftButton.isPressed;
            bool hasClip = _localAudioSource.clip != null;
            
            // Logic: Must be clicking, not using eraser, have a sound clip, and not hovering UI
            return isLeftClick && !_isEraserMode && hasClip && !IsPointerOverUI();
        }

        private void ApplyDrawingAudio(float instantSpeed)
        {
            // Smooth the speed value for less jittery pitch changes (Keep Lerp ONLY for sound)
            float normalizedSpeed = Mathf.InverseLerp(minDrawingSpeed, maxDrawingSpeed, instantSpeed);
            _currentSmoothedSpeed = Mathf.Lerp(_currentSmoothedSpeed, normalizedSpeed, soundSmoothingFactor * Time.deltaTime);

            float globalVolMult = (AudioManager.Instance != null) ? AudioManager.Instance.GetVolumeMult() : 1f;

            _localAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, _currentSmoothedSpeed);
            _localAudioSource.volume = _currentSmoothedSpeed * maxVolume * globalVolMult;
        }

        private void FadeOutAudio()
        {
            _localAudioSource.volume = Mathf.Lerp(_localAudioSource.volume, 0f, 10f * Time.deltaTime);
            _currentSmoothedSpeed = 0f;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Mouse.current == null) return transform.position;

            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = zOffsetFromCamera;
            return _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }

        private void UpdateSystemCursorVisibility()
        {
            bool shouldShowSystemCursor = !(_isEraserMode && !IsPointerOverUI());

            if (_lastCursorVisibleState != shouldShowSystemCursor)
            {
                Cursor.visible = shouldShowSystemCursor;
                _lastCursorVisibleState = shouldShowSystemCursor;
            }
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        #region Event Handlers

        private void RegisterEvents()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnConfigButtonSelected += HandleConfigChanged;
            EventManager.Instance.OnEraserActive += HandleEraserActive;
            EventManager.Instance.OnEraserInactive += HandleEraserInactive;
            EventManager.Instance.OnGamePausedChanged += HandleGamePaused;
        }

        private void UnregisterEvents()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnConfigButtonSelected -= HandleConfigChanged;
            EventManager.Instance.OnEraserActive -= HandleEraserActive;
            EventManager.Instance.OnEraserInactive -= HandleEraserInactive;
            EventManager.Instance.OnGamePausedChanged -= HandleGamePaused;
        }

        private void HandleGamePaused(bool isPaused)
        {
            _isPaused = isPaused;
            if (_isPaused) _localAudioSource.volume = 0;
        }

        private void HandleConfigChanged(object obj)
        {
            HandleEraserInactive();
            /*_isEraserMode = false;
            UpdateAudioClipFromConfig();#1#
        }

        private void HandleEraserActive()
        {
            _isEraserMode = true;
            _localAudioSource.Stop();
            //_localAudioSource.clip = null;
        }

        private void HandleEraserInactive()
        {
            _isEraserMode = false;
            UpdateAudioClipFromConfig();
            if (_localAudioSource.clip != null) _localAudioSource.Play();
        }

        private void UpdateAudioClipFromConfig()
        {
            if (DrawingConfigController.Instance == null || AudioManager.Instance == null) return;

            var config = DrawingConfigController.Instance.currentSettings;
            if (config != null)
            {
                _localAudioSource.clip = AudioManager.Instance.GetClip(config.drawSound);
            }
        }

        #endregion
    }
}*/




/*using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Drawing.LineControl
{
    [RequireComponent(typeof(AudioSource))]
    public class CursorController : MonoBehaviour
    {
        [Header("Debug Settings")] [SerializeField]
        private bool enableDebugLogs = true;

        [Header("Sound Settings")] [SerializeField]
        private float minDrawingSpeed = 0.1f;

        [SerializeField] private float maxDrawingSpeed = 10f;
        [SerializeField] private float minPitch = 0.8f;
        [SerializeField] private float maxPitch = 1f;
        [SerializeField] private float maxVolume = 0.5f;

        [Header("Mouse Following")] [SerializeField]
        private float zOffsetFromCamera = 10f;

        [SerializeField] private float smoothingFactor = 15f;

        private AudioSource _drawingAudioSource;
        private Camera _mainCamera;
        private Vector3 _lastPosition;
        private float _currentSmoothedSpeed;
        private bool _isEraserMode;

        private bool _lastCursorVisibleState = true;
        private bool _isPaused;

        private void Awake()
        {
            _drawingAudioSource = GetComponent<AudioSource>();

            _drawingAudioSource.loop = true;
            _drawingAudioSource.playOnAwake = false;
            _drawingAudioSource.spatialBlend = 0f;
            _drawingAudioSource.volume = 0f;

            if (enableDebugLogs) Debug.Log("[CursorController] Awake: AudioSource initialized.");
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[CursorController] CRITICAL: No Main Camera found!", this);
                enabled = false;
                return;
            }

            UpdateAudioClip();
            _drawingAudioSource.Play();

            _lastPosition = GetMouseWorldPosition();
            transform.position = _lastPosition;
        }

        private void OnEnable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnConfigButtonSelected += HandleConfigChanged;
                EventManager.Instance.OnEraserActive += HandleEraserActive;
                EventManager.Instance.OnEraserInactive += HandleEraserInactive;
                EventManager.Instance.OnGamePausedChanged += HandlePaused;
                if (enableDebugLogs) Debug.Log("[CursorController] Events Subscribed.");
            }
            else
            {
                Debug.LogError("[CursorController] EventManager Instance is NULL inside OnEnable!");
            }
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnConfigButtonSelected -= HandleConfigChanged;
                EventManager.Instance.OnEraserActive -= HandleEraserActive;
                EventManager.Instance.OnEraserInactive -= HandleEraserInactive;
                EventManager.Instance.OnGamePausedChanged -= HandlePaused;
            }

            SetCursorVisibility(true);
            _drawingAudioSource.Stop();
        }

        private void HandlePaused(bool obj)
        {
            _isPaused = obj;
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

        private void Update()
        {
            if (_mainCamera == null || Mouse.current == null || _isPaused)
            {
                UpdateCursorVisibility();
                return; // Safety check for mouse
            }

            // 1. Get current mouse position in world space
            Vector3 currentMouseWorldPos = GetMouseWorldPosition();
            bool hasClip = _drawingAudioSource.clip != null;
            float normalizedSpeed = 0f;
            bool isDrawing = Mouse.current.leftButton.isPressed  && hasClip;

            // 2. Check if the left mouse button is held down (using new Input System)
            if (isDrawing)
            {
                float volmult = 1f;
                if (AudioManager.Instance != null)
                {
                    volmult =  AudioManager.Instance.GetVolumeMult();
                }

                float distance = Vector3.Distance(currentMouseWorldPos, _lastPosition);
                float currentSpeed = distance / Time.deltaTime;

                normalizedSpeed = Mathf.InverseLerp(minDrawingSpeed, maxDrawingSpeed, currentSpeed);
                normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

                _currentSmoothedSpeed = Mathf.Lerp(
                    _currentSmoothedSpeed,
                    normalizedSpeed,
                    smoothingFactor * Time.deltaTime
                );

                _drawingAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, normalizedSpeed);
                _drawingAudioSource.volume = normalizedSpeed * maxVolume * volmult;
            }
            else
            {
                // 3. If mouse button is up, silence the audio
                _drawingAudioSource.volume = 0;
            }

            // 4. Update the object's position to follow the mouse
            transform.position = currentMouseWorldPos;

            // 5. Save the current position for the next frame's speed calculation
            _lastPosition = currentMouseWorldPos;

            UpdateCursorVisibility();

            /*if (Mouse.current == null) return;

            HandleMovementAndSound();
            HandleCursorVisibility();#1#
        }

        private void HandleMovementAndSound()
        {
            Vector3 targetPosition = GetMouseWorldPosition();

            // transform.position = Vector3.Lerp(transform.position, targetPosition, smoothingFactor * Time.deltaTime);

            float distance = Vector3.Distance(targetPosition, _lastPosition);
            float instantaneousSpeed = distance / Time.deltaTime;

            bool isLeftPressed = Mouse.current.leftButton.isPressed;
            bool hasClip = _drawingAudioSource.clip != null;


            /*if (isLeftPressed && !hasClip && enableDebugLogs)
            {
                Debug.LogWarning("[CursorController] Trying to draw but AudioSource has NO CLIP assigned!");
            }#1#

            bool isDrawing = isLeftPressed && !_isEraserMode && hasClip;
            bool overUI = IsPointerOverUI();

            if (isDrawing && !overUI)
            {
                float volMult = AudioManager.Instance.GetVolumeMult();
                float normalizedSpeed = Mathf.InverseLerp(minDrawingSpeed, maxDrawingSpeed, instantaneousSpeed);
                _currentSmoothedSpeed =
                    Mathf.Lerp(_currentSmoothedSpeed, normalizedSpeed, smoothingFactor * Time.deltaTime);

                _drawingAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, _currentSmoothedSpeed);
                _drawingAudioSource.volume  = Mathf.Lerp(0.1f, maxVolume, _currentSmoothedSpeed)* volMult;
                //_drawingAudioSource.volume = _currentSmoothedSpeed * maxVolume * volMult*volMult;


                /*if (enableDebugLogs && _currentSmoothedSpeed > 0.01f)
                {
                   // Debug.Log($"[Drawing] Speed: {instantaneousSpeed:F2} | Vol: {_drawingAudioSource.volume:F2}");
                }#1#
            }
            else
            {
                if (isDrawing && overUI && enableDebugLogs)
                {
                    Debug.Log("[CursorController] Input blocked because pointer is OVER UI.");
                }

                _drawingAudioSource.volume = Mathf.Lerp(_drawingAudioSource.volume, 0f, 10f * Time.deltaTime);
            }

            _lastPosition = targetPosition;
        }

        private void HandleCursorVisibility()
        {
            bool shouldShowSystemCursor = true;

            if (_isEraserMode)
            {
                if (IsPointerOverUI())
                {
                    shouldShowSystemCursor = false;
                }
            }

            SetCursorVisibility(shouldShowSystemCursor);
        }

        private void
            SetCursorVisibility(bool visible)
        {
            if (_lastCursorVisibleState != visible)
            {
                if (enableDebugLogs) Debug.Log($"[CursorController] Changing Cursor Visibility to: {visible}");
                Cursor.visible = visible;
                _lastCursorVisibleState = visible;
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = zOffsetFromCamera;
            return _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }

        // --- Event Handlers ---

        private void HandleConfigChanged(object obj)
        {
            if (enableDebugLogs) Debug.Log("[CursorController] Config Changed Event received.");
            _isEraserMode = false;
            UpdateAudioClip();
        }

        private void HandleEraserActive()
        {
            if (enableDebugLogs) Debug.Log("[CursorController] Eraser Active.");
            _isEraserMode = true;
            if(_drawingAudioSource.isPlaying) _drawingAudioSource.Stop();
            //TODO maybe need to change to eraser sound later
            _drawingAudioSource.clip = AudioManager.Instance.GetClip( GameSoundsSo.AudioType.None);
        }

        private void HandleEraserInactive()
        {
            if (enableDebugLogs) Debug.Log("[CursorController] Eraser Inactive.");
            _isEraserMode = false;
            UpdateAudioClip();
            _drawingAudioSource.Play();
        }

        private void UpdateAudioClip()
        {
            if (DrawingConfigController.Instance == null)
            {
                Debug.LogError("[CursorController] DrawingConfigController Instance is NULL.");
                return;
            }

            var config = DrawingConfigController.Instance.currentSettings;

            if (config != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioClip clip = AudioManager.Instance.GetClip(config.drawSound);
                    _drawingAudioSource.clip = clip;

                    if (enableDebugLogs)
                        Debug.Log($"[CursorController] AudioClip updated to: {(clip != null ? clip.name : "NULL")}");
                }
                else
                {
                    Debug.LogError("[CursorController] AudioManager Instance is NULL, cannot get clip.");
                }
            }
            else
            {
                Debug.LogWarning("[CursorController] CurrentSettings is null in ConfigController.");
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}*/


//______________________________________
/*using System;
using Drawing.Data;
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Drawing.LineControl
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
            AudioManager.Instance.SetBackgroundMusic(config.drawSound);

            _audioSource = AudioManager.Instance.GetBackgroundMusicAudioSource();
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
            }

        }

        private void HandleEraserActive()
        {
            var config = DrawingConfigController.Instance.currentSettings;
            //TODO maybe need to change to eraser sound later
            AudioManager.Instance.SetBackgroundMusic(GameSoundsSo.AudioType.None);
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
            }

            _isEraserMode = false;
            Cursor.visible = true;

        }

        private void HandleButtonPressed(object obj)
        {
            var config = DrawingConfigController.Instance.currentSettings;
            if (config != null)
            {
                AudioManager.Instance.SetBackgroundMusic(config.drawSound);
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
    }
}*/
using Drawing.Managers;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for Controller/Keyboard Navigation

namespace Utilities.UI.PauseMenu
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Input Settings")] [SerializeField]
        private InputActionReference pauseActionReference;

        [Header("UI References")] [SerializeField]
        private GameObject pauseMenuPanel;

        [SerializeField] private Slider musicSlider;
        //[SerializeField] private Slider sfxSlider;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        [Header("Configuration")] [Tooltip("Exact name of the main menu scene.")] [SerializeField]
        private string mainMenuSceneName = "MainMenu";

        private bool _isGamePaused = false;
        private InputAction _pauseAction;

        private void Awake()
        {
            // Fail fast if critical references are missing
            ValidateReferences();

            if (pauseActionReference != null)
            {
                _pauseAction = pauseActionReference.action;
            }
        }

        private void OnEnable()
        {
            BindInput(true);
            BindUI(true);
            Debug.Log("[PauseMenu] OnEnable: Syncing sliders...");
            SyncVolumeSliders();
        }

        private void OnDisable()
        {
            BindInput(false);
            BindUI(false);
        }

        private void Start()
        {
            SyncVolumeSliders();
            // Ensure UI state is correct on startup
            pauseMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Centralized binding for Input System to prevent logic duplication.
        /// </summary>
        private void BindInput(bool bind)
        {
            if (_pauseAction == null) return;

            if (bind)
            {
                _pauseAction.Enable();
                _pauseAction.performed += OnPausePerformed;
            }
            else
            {
                _pauseAction.performed -= OnPausePerformed;
                _pauseAction.Disable();
            }
        }

        /// <summary>
        /// Centralized binding for UI Elements.
        /// </summary>
        private void BindUI(bool bind)
        {
            if (bind)
            {
                if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
                if (quitButton) quitButton.onClick.AddListener(QuitGame);
                if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        //        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            else
            {
                if (resumeButton) resumeButton.onClick.RemoveListener(ResumeGame);
                if (quitButton) quitButton.onClick.RemoveListener(QuitGame);
                if (musicSlider) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
         //       if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[PauseMenu] Input Received: Toggle Pause");
            TogglePauseState();
        }

        public void TogglePauseState()
        {
            if (_isGamePaused) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            Debug.Log("[PauseMenu] Pausing Game...");
            _isGamePaused = true;
            pauseMenuPanel.SetActive(true);
            SyncVolumeSliders();
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerGamePaused(true);
            }

// CRITICAL: Ensure sliders match current audio levels in case they changed elsewhere
            

            Time.timeScale = 0f;

/*// Accessibility: Select the Resume button for Gamepad/Keyboard navigation
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }*/
        }


        public void ResumeGame()
        {
            _isGamePaused = false;
            Debug.Log("[PauseMenu] Resuming Game...");
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f;

// Optional: clear selection so buttons don't stay highlighted in world space
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

  
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerGamePaused(false);
            }
        }

        public void QuitGame()
        {
// Always reset time scale before leaving a scene
            Time.timeScale = 1f;
            Debug.Log($"[PauseMenu] Quitting Game to scene: {mainMenuSceneName}");
            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogError($"[PauseMenu] Scene '{mainMenuSceneName}' cannot be loaded. Check Build Settings.");
            }
        }

        private void SyncVolumeSliders()
        {
// Null-conditional operator to clean up the singleton check
            if (AudioManager.Instance == null)
            {
                Debug.LogError($"[PauseMenu] Audio Manager is not assigned!", this);
                return;
            }

            if (musicSlider) musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolumeMult());
      //      if (sfxSlider) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());
        }

        private void OnMusicVolumeChanged(float volume)
        {
            AudioManager.Instance?.SetVolumeMult(volume);
        }

        private void OnSFXVolumeChanged(float volume)
        {
            AudioManager.Instance?.SetSFXVolume(volume);
        }

        private void ValidateReferences()
        {
            if (pauseMenuPanel == null) Debug.LogError("PauseMenuPanel is not assigned!", this);
            if (pauseActionReference == null) Debug.LogError("Pause Action Reference is missing!", this);
        }
    }
}
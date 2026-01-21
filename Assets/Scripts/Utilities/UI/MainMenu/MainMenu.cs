using System.Collections;
using Drawing.Data;
using Drawing.Managers.Core.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utilities.UI.MainMenu
{
    /// <summary>
    /// Controls the main menu behavior, including starting the game and displaying a loading progress bar.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private GameObject loadingScreen; // The loading screen GameObject

        /// <summary>
        /// The panel containing the main menu UI elements.
        /// </summary>
        [SerializeField] private Transform mainMenuPanel;

        /// <summary>
        /// The UI Image component used as the loading progress bar fill (Fill type).
        /// </summary>
        [SerializeField] private Image progressBarFill;

        /// <summary>
        /// Maximum fake loading progress (0.1 to 0.99) before performing real scene loading.
        /// </summary>
        [Range(0.1f, 0.99f)] [SerializeField] private float maxFakeProgress = 0.75f;

        /// <summary>
        /// Time in seconds to simulate fake loading until reaching maxFakeProgress.
        /// </summary>
        [SerializeField] private float fakeSpeed = 0.6f;

        /// <summary>
        /// The Start Game button in the main menu.
        /// </summary>
        [SerializeField] private Button startGameButton;

        /// <summary>
        /// The name of the scene to load when pressing START GAME.
        /// </summary>
        [Tooltip("The name of the scene to load when pressing START GAME")] [SerializeField]
        private string gameSceneName = "Level-1";

        #endregion

        #region Private Fields

        /// <summary>
        /// Flag indicating whether the game is currently loading.
        /// </summary>
        private bool _isLoading;

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Unity Start callback. Sets up button listener.
        /// </summary>
        private void Start()
        {
            if (startGameButton != null)
            {
                // Hide loading screen at start
                loadingScreen.SetActive(false);
                // Add listener to the start button
                startGameButton.onClick.AddListener(OnStartButtonClicked);

                // Register StartGame to button click
                //startGameButton.onClick.AddListener(StartGame);
            }
            else
            {
                Debug.LogWarning("StartGameButton is not assigned in the inspector!");
            }
            /*AudioManager.instance.Play("MainMenuBGMusic");*/
        }


        private void OnStartButtonClicked()
        {
            if (_isLoading)
                return;

            
            // Play button click sound effect
            AudioManager.Instance.PlaySoundByAudioType(GameSoundsSo.AudioType.ButtonClick);

            // Hide main menu UI
            if (mainMenuPanel != null)
                mainMenuPanel.gameObject.SetActive(false);
            _isLoading = true;
            loadingScreen.SetActive(true);
            // disable the start button to prevent multiple clicks
            startGameButton.interactable = false;
            StartGame();
        }


        /*/// <summary>
    /// Unity Update callback. Checks for input to start the game (disabled by default).
    /// </summary>
    private void Update()
    {
      //  if (_isLoading) return;

    }*/

        #endregion

        #region Public Methods

        /// <summary>
        /// Begins the asynchronous loading of the game scene with a fake loading phase.
        /// </summary>
        public void StartGame()
        {
            // Start coroutine to load the game scene
            StartCoroutine(LoadGameSceneAsync());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine that handles fake loading, real asynchronous loading, and progress bar updates.
        /// </summary>
        private IEnumerator LoadGameSceneAsync()
        {
            float fakeProgress = 0f;

            // Initialize progress bar to zero if assigned
            if (progressBarFill != null)
                progressBarFill.fillAmount = 0f;

            // Phase 1: Fake loading until reaching maxFakeProgress
            while (fakeProgress < maxFakeProgress)
            {
                fakeProgress += Time.deltaTime / fakeSpeed;
                if (progressBarFill != null)
                    progressBarFill.fillAmount = fakeProgress;
                yield return null;
            }

            // Reset progress bar before real loading
            if (progressBarFill != null)
                progressBarFill.fillAmount = 0f;

            // Phase 2: Actual asynchronous scene loading
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
            if (asyncLoad == null)
            {
                Debug.LogWarning("Uncorrected Scene name. Please check the gameSceneName variable.");
                yield break;
            }

            asyncLoad.allowSceneActivation = false;

            // Wait until loading reaches 90% (Unity's async threshold)
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            // Phase 3: Animate progress bar from fake max to full
            float realProgress = maxFakeProgress;
            while (realProgress < 1f)
            {
                // Increase progress to full over 0.2 seconds
                realProgress += Time.deltaTime / 0.2f;
                if (progressBarFill != null)
                    progressBarFill.fillAmount = Mathf.Min(1f, realProgress);
                yield return null;
            }
            /*AudioManager.instance.Stop("MainMenuBGMusic"); // Stop main menu music*/

            // Ensure progress bar is full
            if (progressBarFill != null)
                progressBarFill.fillAmount = 1f;

            // Short delay to display full bar
            yield return new WaitForSeconds(0.2f);

            // Allow scene activation to complete load
            asyncLoad.allowSceneActivation = true;
        }

        #endregion
    }
}
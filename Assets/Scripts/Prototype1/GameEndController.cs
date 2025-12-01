using System.Collections;
using Drawing.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prototype1
{
    /// <summary>
    /// Listens for the global game-finished event, shows a UI message, and ends the scene after a delay.
    /// Attach this to a persistent object (e.g., GameManager) and assign the UI canvas that says "נגמר המשחק".
    /// </summary>
    public class GameEndController : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Canvas or panel that contains the end-of-game message.")]
        [SerializeField] private GameObject gameEndCanvas;

        [Header("Scene Flow")]
        [Tooltip("Seconds to wait before performing the end-of-scene action.")]
        [SerializeField] private float endDelaySeconds = 5f;
        [Tooltip("What should happen once the delay is over.")]
        [SerializeField] private GameEndAction endAction = GameEndAction.ReloadCurrentScene;
        [Tooltip("Scene name to load when using LoadSpecificScene.")]
        [SerializeField] private string sceneToLoad;

        private Coroutine _endRoutine;

        private void Awake()
        {
            if (gameEndCanvas != null)
            {
                gameEndCanvas.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished += HandleGameFinished;
            }
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished -= HandleGameFinished;
            }
        }

        private void HandleGameFinished()
        {
            if (_endRoutine != null)
            {
                return;
            }

            if (gameEndCanvas != null)
            {
                gameEndCanvas.SetActive(true);
            }

            _endRoutine = StartCoroutine(EndSceneAfterDelay());
        }

        private IEnumerator EndSceneAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, endDelaySeconds));

            switch (endAction)
            {
                case GameEndAction.ReloadCurrentScene:
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
                case GameEndAction.LoadSpecificScene:
                    if (!string.IsNullOrWhiteSpace(sceneToLoad))
                    {
                        SceneManager.LoadScene(sceneToLoad);
                    }
                    break;
            }
        }
    }

    public enum GameEndAction
    {
        ReloadCurrentScene,
        LoadSpecificScene
    }
}

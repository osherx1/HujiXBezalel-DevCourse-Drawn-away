using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal start screen controller:
/// - Play button loads the gameplay scene
/// - Menu button loads a menu/options scene
/// Hook these methods to UI Button OnClick events.
/// </summary>
[DisallowMultipleComponent]
public class StartMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Scene to load when pressing PLAY.")]
    [SerializeField] private string playSceneName = "Game";

    [Tooltip("Scene to load when pressing MENU.")]
    [SerializeField] private string menuSceneName = "Menu";

    public void Play()
    {
        LoadSceneByName(playSceneName);
    }

    public void Menu()
    {
        LoadSceneByName(menuSceneName);
    }

    private static void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("StartMenuController: Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}

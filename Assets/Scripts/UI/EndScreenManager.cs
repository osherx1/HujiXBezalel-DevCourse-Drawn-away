using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreenManager : MonoBehaviour
{
    [Header("Navigation Settings")]
    [Tooltip("The exact name of your first level or Main Menu scene.")]
    [SerializeField] private string startSceneName = "Level1"; 

    [Header("Buttons")]
    [SerializeField] private Button startOverButton;
    [SerializeField] private Button exitButton;

    private void Start()
    {
        // Auto-assign listeners
        if (startOverButton != null)
            startOverButton.onClick.AddListener(OnStartOverClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    public void OnStartOverClicked()
    {
        // Reset any persistent game state if you have singletons (Optional)
        // Example: DrawingConfigController.Instance.ResetInk(); 
        
        // Load the game scene
        SceneManager.LoadScene(startSceneName);
    }

    public void OnExitClicked()
    {
        Debug.Log("Exit Button Clicked! (Application will close in build)");
        Application.Quit();
        
#if UNITY_EDITOR
        // Useful for testing in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
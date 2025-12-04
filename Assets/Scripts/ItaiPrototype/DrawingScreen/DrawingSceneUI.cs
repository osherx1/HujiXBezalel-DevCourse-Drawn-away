using ItaiPrototype.Utilities;
using TMPro;
using UnityEngine;

// Or TMPro if you use TextMeshPro

namespace ItaiPrototype.DrawingScreen
{
    public class DrawingSceneUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText; // Drag your UI Text here

        private void Start()
        {
            // Pull the prompt from the current level data
            if (GameManager.Instance != null)
            {
                promptText.text = GameManager.Instance.GetCurrentLevelData().prompt;
            }
        }
    }
}
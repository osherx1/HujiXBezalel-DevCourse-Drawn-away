using TMPro;
using UnityEngine;

// Or TMPro if you use TextMeshPro

namespace ItaiPrototype
{
    public class DrawingSceneUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText; // Drag your UI Text here

        private void Start()
        {
            // Pull the prompt from the current level data
            if (GameManager.instance != null)
            {
                promptText.text = GameManager.instance.GetCurrentLevelData().prompt;
            }
        }
    }
}
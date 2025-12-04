using UnityEngine;
using UnityEngine.UI;

// Or TMPro if you use TextMeshPro

namespace ItaiPrototype
{
    public class DrawingSceneUI : MonoBehaviour
    {
        [SerializeField] private Text promptText; // Drag your UI Text here

        void Start()
        {
            // Pull the prompt from the current level data
            if (GameManager.instance != null)
            {
                promptText.text = GameManager.instance.GetCurrentLevelData().prompt;
            }
        }
    }
}
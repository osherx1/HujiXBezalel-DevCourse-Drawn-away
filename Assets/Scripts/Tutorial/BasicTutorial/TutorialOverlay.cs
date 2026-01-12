using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlay : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject overlayPanel; // The full-screen dark panel
    [SerializeField] private RectTransform spotlightMask; // The object with the Mask component
    [SerializeField] private Image instructionImageRenderer; // The UI Image that will show your sprite
    [SerializeField] private Camera mainCamera;

    [Header("Settings")]
    [SerializeField] private Vector2 defaultImageOffset = new Vector2(0, 100); // Offset from center if no target

    public void ShowFocus(Transform targetWorldObject, Sprite instructionSprite)
    {
        overlayPanel.SetActive(true);

        // 1. Set the Instruction Sprite
        if (instructionSprite != null)
        {
            instructionImageRenderer.sprite = instructionSprite;
            instructionImageRenderer.gameObject.SetActive(true);
            instructionImageRenderer.SetNativeSize(); // Optional: ensures sprite isn't stretched
        }
        else
        {
            instructionImageRenderer.gameObject.SetActive(false);
        }

        // 2. Handle Spotlight Position
        if (targetWorldObject != null)
        {
            // Convert world position (the pickup/door) to screen UI position
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldObject.position);
            spotlightMask.position = screenPos;
            spotlightMask.gameObject.SetActive(true);
        }
        else
        {
            // If no specific target (e.g., just movement), hide the spotlight hole
            spotlightMask.gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        overlayPanel.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro; // Assuming you have DOTween based on your previous file

public class TutorialOverlay : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject overlayPanel; // The full-screen dark panel
    [SerializeField] private RectTransform spotlightMask; // The object with the Mask component
    [SerializeField] private TextMeshProUGUI instructionText; // Text to tell player what to do
    [SerializeField] private Camera mainCamera;

    public void ShowFocus(Transform targetWorldObject, string text)
    {
        overlayPanel.SetActive(true);
        instructionText.text = text;

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
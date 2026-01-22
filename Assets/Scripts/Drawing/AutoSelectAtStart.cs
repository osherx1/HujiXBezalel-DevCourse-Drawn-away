using System.Collections;
using Drawing;
using UnityEngine;
using UnityEngine.UI;
using Drawing.Buttons;

public class AutoSelectAtStart : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Short delay in seconds before the action starts (useful to ensure initialization).")]
    [SerializeField] private float delaySeconds = 0.1f;

    [Header("References")]
    [Tooltip("The script responsible for opening the menu.")]
    [SerializeField] private MenuController menuController;

    [Header("Target Selection")]
    [Tooltip("The specific tool/config button to select. This is the primary target.")]
    [SerializeField] private DrawingConfigButton toolToSelect;

    [Tooltip("Fallback: If no tool is assigned above, this standard button will be clicked.")]
    [SerializeField] private Button fallbackButton;

    private IEnumerator Start()
    {
        // Optional wait to ensure UI systems are fully loaded
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        // 1. Open the menu first
        OpenTheMenu();

        // 2. Select the target button/tool
        SelectTarget();
    }

    private void OpenTheMenu()
    {
        // If MenuController wasn't assigned in the Inspector, try to find it automatically
        if (menuController == null)
        {
            menuController = FindObjectOfType<MenuController>(true);
        }

        if (menuController != null)
        {
            menuController.OpenMenu();
        }
        else
        {
            Debug.LogWarning("AutoSelectAtStart: No MenuController found!");
        }
    }

    private void SelectTarget()
    {
        // Priority 1: Select the specific tool
        if (toolToSelect != null)
        {
            // The 'false' parameter usually means "don't toggle off if already selected"
            toolToSelect.SelectTool(false);
            return;
        }

        // Priority 2: Click a standard UI button
        if (fallbackButton != null)
        {
            fallbackButton.onClick.Invoke();
        }
    }
}
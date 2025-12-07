namespace Drawing.Buttons
{
    using UnityEngine;

    public class MenuController : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private GameObject menuPanel;

        [Header("Time Settings")] [Tooltip("If true, time will stop completely when menu is open.")] [SerializeField]
        private bool pauseGameOnOpen = true;

        [Tooltip("If true (and pause is false), time will slow down instead of stopping.")] [SerializeField]
        private bool slowTimeOnOpen = false;

        [Tooltip("The time scale value when slowed down (0.0 to 1.0).")] [SerializeField, Range(0f, 1f)]
        private float slowMotionFactor = 0.5f;

        private void Awake()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }

            // Ensure time is running normally at start
            Time.timeScale = 1f;
        }

        public void ToggleMenu()
        {
            if (menuPanel == null) return;

            // Determine the new state (if active, we act to close it, and vice versa)
            bool isCurrentlyActive = menuPanel.activeSelf;
            bool newState = !isCurrentlyActive;

            menuPanel.SetActive(newState);
            UpdateTimeScale(newState);
        }

        private void UpdateTimeScale(bool isMenuOpen)
        {
            if (isMenuOpen)
            {
                if (pauseGameOnOpen)
                {
                    Time.timeScale = 0f;
                }
                else if (slowTimeOnOpen)
                {
                    Time.timeScale = slowMotionFactor;
                }
            }
            else
            {
                // Reset to normal time when closing
                Time.timeScale = 1f;
            }
        }
    }
}
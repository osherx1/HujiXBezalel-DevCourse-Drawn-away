using System;

namespace Drawing.Buttons
{
    using UnityEngine;
    using System.Collections;

    public class MenuController : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private GameObject menuPanel;

        [Header("Time Settings")] 
        [Tooltip("If true, time will stop completely (target scale 0).")] 
        [SerializeField] private bool pauseGameOnOpen = true;

        [Tooltip("If true (and pause is false), time will slow down to the factor below.")] 
        [SerializeField] private bool slowTimeOnOpen;

        [Tooltip("The time scale value when slowed down (0.0 to 1.0).")] 
        [SerializeField, Range(0f, 1f)] private float slowMotionFactor = 0.5f;

        [Header("Transition Settings")]
        [Tooltip("How long (in real seconds) the transition to/from slow motion takes.")]
        [SerializeField] private float transitionDuration = 0.5f;

        // Event to notify the effects manager that the state has changed.
        // The boolean parameter indicates: Are we entering slow motion/pause?
        public Action<bool> onTimeStateChanged; 

        private Coroutine _timeCoroutine;
        private float _defaultFixedDeltaTime; // To maintain physics stability

        private void Awake()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            
            // Ensure time is running normally at start
            Time.timeScale = 1f;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        public void ToggleMenu()
        {
            if (menuPanel == null) return;

            bool isCurrentlyActive = menuPanel.activeSelf;
            bool newState = !isCurrentlyActive; // The new state we are transitioning to

            menuPanel.SetActive(newState);
            
            // Update the time scale
            UpdateTimeScale(newState);
            
            // Trigger effects (connect to Effects Manager)
            onTimeStateChanged?.Invoke(newState);
        }

        private void UpdateTimeScale(bool isMenuOpen)
        {
            float targetTimeScale = 1f;

            if (isMenuOpen)
            {
                if (pauseGameOnOpen) targetTimeScale = 0f;
                else if (slowTimeOnOpen) targetTimeScale = slowMotionFactor;
            }

            // If a time transition is already running, stop it and start a new one
            if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);
            _timeCoroutine = StartCoroutine(SmoothTimeTransition(targetTimeScale));
        }

        private IEnumerator SmoothTimeTransition(float targetScale)
        {
            float startScale = Time.timeScale;
            float timer = 0f;

            while (timer < transitionDuration)
            {
                // Important: Use unscaledDeltaTime because if time stops, 
                // normal deltaTime becomes 0 and the loop will freeze.
                timer += Time.unscaledDeltaTime;
                
                float t = timer / transitionDuration;
                
                // Use SmoothStep for a smoother transition than standard linear interpolation
                t = Mathf.SmoothStep(0f, 1f, t); 

                Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
                
                // Adjust physics to prevent jittering in slow motion
                Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;

                yield return null;
            }

            // Ensure we reach the exact final value
            Time.timeScale = targetScale;
            Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;
        }
    }
}


/*namespace Drawing.Buttons
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
}*/
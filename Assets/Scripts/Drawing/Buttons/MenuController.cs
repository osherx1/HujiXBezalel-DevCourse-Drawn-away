using System;
using System.Collections;
using CustomInspector;
using Drawing.Managers;
using UnityEngine;

namespace Drawing.Buttons
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private GameObject menuPanel;

        [Header("Time Settings")] 
        [Tooltip("If true, time will stop completely (target scale 0).")] 
        [Hook(nameof(IgnoreSlowMotionSettings))]
        [SerializeField] private bool pauseGameOnOpen = true;

        [Tooltip("If true (and pause is false), time will slow down to the factor below.")] 
        [ShowIfNot(nameof(pauseGameOnOpen))] 
        [SerializeField] private bool slowTimeOnOpen;

        [Tooltip("The time scale value when slowed down (0.0 to 1.0).")] 
        [ShowIfNot(nameof(pauseGameOnOpen))] [ShowIf(nameof(slowTimeOnOpen))] [Indent(1)]
        [SerializeField, Range(0f, 1f)] private float slowMotionFactor = 0.5f;

        [Header("Transition Settings")]
        [Tooltip("How long (in real seconds) the transition to/from slow motion takes.")]
        [ShowIfNot(nameof(pauseGameOnOpen))][ShowIf(nameof(slowTimeOnOpen))] [Indent(1)]
        [SerializeField] private float transitionDuration = 0.5f;

        private Coroutine _timeCoroutine;
        private Coroutine _closeSequenceCoroutine; // Reference to the delayed close routine
        private float _defaultFixedDeltaTime; 

        private void Awake()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            
            Time.timeScale = 1f;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        public void OpenMenu() => SetMenuState(true);

        public void CloseMenu() => SetMenuState(false);

        /// <summary>
        /// Closes the menu after a specific delay (useful for button animations/sounds).
        /// Assign this to a UI Button OnClick event.
        /// </summary>
        /// <param name="delay">Time in real seconds to wait before closing.</param>
        public void CloseMenuWithDelay(float delay)
        {
            // If no delay, close immediately
            if (delay <= 0f)
            {
                CloseMenu();
                return;
            }

            // Stop any existing close sequence to avoid conflicts
            if (_closeSequenceCoroutine != null) StopCoroutine(_closeSequenceCoroutine);
            
            _closeSequenceCoroutine = StartCoroutine(WaitAndCloseRoutine(delay));
        }

        public void ToggleMenu()
        {
            if (menuPanel == null) return;
            SetMenuState(!menuPanel.activeSelf);
        }

        private void SetMenuState(bool isOpen)
        {
            if (menuPanel == null) return;

            // If we are forcing a state change, cancel any pending delayed close
            if (_closeSequenceCoroutine != null)
            {
                StopCoroutine(_closeSequenceCoroutine);
                _closeSequenceCoroutine = null;
            }

            if (menuPanel.activeSelf == isOpen) return;

            menuPanel.SetActive(isOpen);
            
            UpdateTimeScale(isOpen);
            
            EventManager.Instance.TriggerSlowMotion(isOpen && slowTimeOnOpen);
            EventManager.Instance.TriggerGamePaused(isOpen && pauseGameOnOpen);
        }

        private IEnumerator WaitAndCloseRoutine(float delay)
        {
            // Use Realtime because the game might be paused (Time.timeScale = 0)
            yield return new WaitForSecondsRealtime(delay);
            
            CloseMenu();
            _closeSequenceCoroutine = null;
        }

        private void UpdateTimeScale(bool isMenuOpen)
        {
            float targetTimeScale = 1f;

            if (isMenuOpen)
            {
                if (pauseGameOnOpen) targetTimeScale = 0f;
                else if (slowTimeOnOpen) targetTimeScale = slowMotionFactor;
            }

            if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);
            _timeCoroutine = StartCoroutine(SmoothTimeTransition(targetTimeScale));
        }

        private IEnumerator SmoothTimeTransition(float targetScale)
        {
            float startScale = Time.timeScale;
            float timer = 0f;

            while (timer < transitionDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration); 

                Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
                Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;

                yield return null;
            }

            Time.timeScale = targetScale;
            Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;
        }

        public void IgnoreSlowMotionSettings()
        {
            if(pauseGameOnOpen) slowTimeOnOpen = false;
        }
    }
}

/*
using System;
using CustomInspector;
using Drawing.Managers;
using UnityEngine;
using System.Collections;

namespace Drawing.Buttons
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private GameObject menuPanel;

        [Header("Time Settings")] 
        [Tooltip("If true, time will stop completely (target scale 0).")] 
        [Hook(nameof(IgnoreSlowMotionSettings))]
        [SerializeField] private bool pauseGameOnOpen = true;

        [Tooltip("If true (and pause is false), time will slow down to the factor below.")] 
        [ShowIfNot(nameof(pauseGameOnOpen))] 
        [SerializeField] private bool slowTimeOnOpen;

        [Tooltip("The time scale value when slowed down (0.0 to 1.0).")] 
        [ShowIfNot(nameof(pauseGameOnOpen))] [ShowIf(nameof(slowTimeOnOpen))] [Indent(1)]
        [SerializeField, Range(0f, 1f)] private float slowMotionFactor = 0.5f;

        [Header("Transition Settings")]
        [Tooltip("How long (in real seconds) the transition to/from slow motion takes.")]
        [ShowIfNot(nameof(pauseGameOnOpen))][ShowIf(nameof(slowTimeOnOpen))] [Indent(1)]
        [SerializeField] private float transitionDuration = 0.5f;

        private Coroutine _timeCoroutine;
        private float _defaultFixedDeltaTime; // To maintain physics stability

        private void Awake()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            
            // Ensure time is running normally at start
            Time.timeScale = 1f;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        /// <summary>
        /// Explicitly opens the menu.
        /// </summary>
        public void OpenMenu()
        {
            SetMenuState(true);
        }

        /// <summary>
        /// Explicitly closes the menu.
        /// </summary>
        public void CloseMenu()
        {
            SetMenuState(false);
        }

        /// <summary>
        /// Toggles the menu state based on current status.
        /// </summary>
        public void ToggleMenu()
        {
            if (menuPanel == null) return;
            SetMenuState(!menuPanel.activeSelf);
        }

        /// <summary>
        /// Centralized logic for setting menu state to avoid code duplication.
        /// </summary>
        private void SetMenuState(bool isOpen)
        {
            if (menuPanel == null) return;

            // Optimization: If the state is not changing, do nothing
            if (menuPanel.activeSelf == isOpen) return;

            menuPanel.SetActive(isOpen);
            
            // Update the time scale
            UpdateTimeScale(isOpen);
            
            // Trigger Events
            EventManager.Instance.TriggerSlowMotion(isOpen && slowTimeOnOpen);
            EventManager.Instance.TriggerGamePaused(isOpen && pauseGameOnOpen);
        }

        private void UpdateTimeScale(bool isMenuOpen)
        {
            float targetTimeScale = 1f;

            if (isMenuOpen)
            {
                if (pauseGameOnOpen) targetTimeScale = 0f;
                else if (slowTimeOnOpen)
                {
                    targetTimeScale = slowMotionFactor;
                }
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

        public void IgnoreSlowMotionSettings()
        {
            if(pauseGameOnOpen)
            {
                slowTimeOnOpen = false;
            }
        }
    }
}
*/



/*
using System;
using CustomInspector;
using Drawing.Managers;

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
        [Hook(nameof(IgnoreSlowMotionSettings))]
        [SerializeField] private bool pauseGameOnOpen = true;

        [Tooltip("If true (and pause is false), time will slow down to the factor below.")] 
        [ShowIfNot(nameof(pauseGameOnOpen))] 
        
        [SerializeField] private bool slowTimeOnOpen;

        [Tooltip("The time scale value when slowed down (0.0 to 1.0).")] 
        
        [ShowIfNot(nameof(pauseGameOnOpen))] [ShowIf(nameof(slowTimeOnOpen))] [Indent(1)]
        [SerializeField, Range(0f, 1f)] private float slowMotionFactor = 0.5f;

        [Header("Transition Settings")]
        [Tooltip("How long (in real seconds) the transition to/from slow motion takes.")]
        [ShowIfNot(nameof(pauseGameOnOpen))][ShowIf(nameof(slowTimeOnOpen))] [Indent(1)]
        [SerializeField] private float transitionDuration = 0.5f;

        // Event to notify the effects manager that the state has changed.
        // The boolean parameter indicates: Are we entering slow motion/pause?
        //public Action<bool> onTimeStateChanged; 

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
            EventManager.Instance.TriggerSlowMotion(newState&&slowTimeOnOpen);
            EventManager.Instance.TriggerGamePaused(newState&&pauseGameOnOpen);

            // Trigger effects (connect to Effects Manager)
            //onTimeStateChanged?.Invoke(newState);
        }

        private void UpdateTimeScale(bool isMenuOpen)
        {
            float targetTimeScale = 1f;

            if (isMenuOpen)
            {
                if (pauseGameOnOpen) targetTimeScale = 0f;
                else if (slowTimeOnOpen)
                {
                    targetTimeScale = slowMotionFactor;

                }
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
        public void IgnoreSlowMotionSettings()
        {
            if(pauseGameOnOpen)
            {
                slowTimeOnOpen = false;
            }
        }
        //if pause is true, slow motion settings are ignored

        public void OpenMenu()
        {
            throw new NotImplementedException();
        }

        public void CloseMenu()
        {
            throw new NotImplementedException();
        }
    }
    */

    
    
    
    
    
    
    
    
    
    
    
    
    



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
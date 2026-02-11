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

        [Header("Auto Close Settings")]
        [Tooltip("If true, the menu will automatically close after the duration.")]
        [SerializeField] private bool enableAutoClose = false;
        [SerializeField] private bool startMenuOpen = false;

        [Tooltip("Time in real seconds before the menu closes automatically.")]
        [ShowIf(nameof(enableAutoClose))] [Indent(1)]
        [SerializeField] private float autoCloseDelay = 3.0f;

        private Coroutine _timeCoroutine;
        private Coroutine _closeSequenceCoroutine; // Reference to the button-delayed close routine
        private Coroutine _autoCloseCoroutine;     // Reference to the idle auto-close routine
        private float _defaultFixedDeltaTime; 

        private void Awake()
        {
            if (menuPanel != null) menuPanel.SetActive(startMenuOpen);
            
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

            // --- COROUTINE MANAGEMENT ---
            
            // 1. If we are changing state manually, cancel any "Button Delay" close
            if (_closeSequenceCoroutine != null)
            {
                StopCoroutine(_closeSequenceCoroutine);
                _closeSequenceCoroutine = null;
            }

            // 2. Always stop the Auto-Close timer when state changes or re-evaluates.
            //    If opening, we restart it below. If closing, we want it gone.
            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }

            // Optimization: If state matches, we don't need to do UI/Time logic,
            // BUT if we just opened it, we might want to restart the auto-close timer? 
            // For now, we return to avoid re-triggering events.
            if (menuPanel.activeSelf == isOpen) return;

            // --- STATE EXECUTION ---

            menuPanel.SetActive(isOpen);
            
            /*UpdateTimeScale(isOpen);
            
            EventManager.Instance.TriggerSlowMotion(isOpen && slowTimeOnOpen);
            EventManager.Instance.TriggerGamePaused(isOpen && pauseGameOnOpen);*/

            // 3. Start Auto-Close if opening
            if (isOpen && enableAutoClose)
            {
                _autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
            }
        }

        private IEnumerator AutoCloseRoutine()
        {
            // We use Realtime because pauseGameOnOpen might be set to true, 
            // which sets Time.timeScale to 0. WaitForSeconds would hang forever.
            yield return new WaitForSecondsRealtime(autoCloseDelay);
            
            CloseMenu();
            _autoCloseCoroutine = null;
        }

        private IEnumerator WaitAndCloseRoutine(float delay)
        {
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
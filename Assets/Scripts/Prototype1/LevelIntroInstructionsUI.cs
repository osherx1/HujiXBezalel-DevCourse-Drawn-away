using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Prototype1
{
    /// <summary>
    /// Shows a UI instructions panel at level start, then fades it out.
    /// While visible (including fade), player movement is locked.
    /// </summary>
    public class LevelIntroInstructionsUI : MonoBehaviour
    {
        public enum DismissMode
        {
            Timed,
            PressAnyButton
        }

        [Header("UI")]
        [Tooltip("CanvasGroup controlling the instructions panel.")]
        [SerializeField] private CanvasGroup instructionsGroup;

        [Header("Press Any Button UI")]
        [Tooltip("Optional: assign the 'PRESS ANY BUTTON' text GameObject here (can start inactive).")]
        [SerializeField] private GameObject pressAnyButtonObject;

        [Tooltip("Optional: CanvasGroup on the 'PRESS ANY BUTTON' object (preferred for blinking).")]
        [SerializeField] private CanvasGroup pressAnyButtonCanvasGroup;

        [Tooltip("Optional: UI Graphic (Text/Image) to blink by changing its alpha if no CanvasGroup is provided.")]
        [SerializeField] private Graphic pressAnyButtonGraphic;

        [Tooltip("If enabled, the PRESS ANY BUTTON UI will blink while waiting for input.")]
        [SerializeField] private bool blinkPressAnyButton = true;

        [Min(0.05f)]
        [SerializeField] private float blinkPeriodSeconds = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float blinkMinAlpha = 0.2f;

        [Range(0f, 1f)]
        [SerializeField] private float blinkMaxAlpha = 1f;

        [Header("Timing")]
        [SerializeField] private DismissMode dismissMode = DismissMode.Timed;

        [Min(0f)]
        [SerializeField] private float showSeconds = 5.5f;

        [Tooltip("Optional: minimum seconds to show before accepting input.")]
        [Min(0f)]
        [SerializeField] private float minSecondsBeforeInput = 5f;

        [Min(0f)]
        [SerializeField] private float fadeOutSeconds = 0.6f;

        [Tooltip("If true, timing uses real time (unscaled). Recommended for UI.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Player Lock")]
        [SerializeField] private movementLimiter movementGate;
        [SerializeField] private characterMovement movementController;

        [Tooltip("Optional: also freeze Rigidbody2D simulation during the intro.")]
        [SerializeField] private Rigidbody2D playerBody;

        [SerializeField] private bool freezeRigidbody = true;

        private Coroutine _routine;
        private Coroutine _blinkRoutine;
        private bool _bodyWasSimulated;
        private bool _wasLocked;
        private bool _inputReceived;
        private System.IDisposable _anyButtonPressListener;

        private bool _pressAnyWasActive;
        private bool _pressAnyCanvasWasActive;
        private float _pressAnyCanvasOriginalAlpha;
        private Color _pressAnyGraphicOriginalColor;

        private sealed class AnyButtonObserver : IObserver<InputControl>
        {
            private readonly LevelIntroInstructionsUI _owner;

            public AnyButtonObserver(LevelIntroInstructionsUI owner)
            {
                _owner = owner;
            }

            public void OnNext(InputControl value)
            {
                _owner._inputReceived = true;
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }

        private void Reset()
        {
            if (instructionsGroup == null)
            {
                instructionsGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            if (pressAnyButtonObject == null)
            {
                pressAnyButtonObject = null;
            }
        }

        private void OnEnable()
        {
            StartIntro();
        }

        private void OnDisable()
        {
            CleanupAndUnlock();
        }

        public void StartIntro()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }

            _inputReceived = false;
            _anyButtonPressListener?.Dispose();
            _anyButtonPressListener = null;

            if (instructionsGroup == null)
            {
                return;
            }

            // Ensure visible immediately
            if (!instructionsGroup.gameObject.activeSelf)
            {
                instructionsGroup.gameObject.SetActive(true);
            }

            instructionsGroup.alpha = 1f;
            instructionsGroup.interactable = false;
            instructionsGroup.blocksRaycasts = false;

            PreparePressAnyButtonUI();

            LockPlayer();

            _routine = StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            if (dismissMode == DismissMode.Timed)
            {
                if (showSeconds > 0f)
                {
                    if (useUnscaledTime)
                    {
                        yield return new WaitForSecondsRealtime(showSeconds);
                    }
                    else
                    {
                        yield return new WaitForSeconds(showSeconds);
                    }
                }
            }
            else
            {
                if (minSecondsBeforeInput > 0f)
                {
                    if (useUnscaledTime)
                    {
                        yield return new WaitForSecondsRealtime(minSecondsBeforeInput);
                    }
                    else
                    {
                        yield return new WaitForSeconds(minSecondsBeforeInput);
                    }
                }

                // Start listening for input only after the lockout period.
                _inputReceived = false;
                _anyButtonPressListener?.Dispose();
                _anyButtonPressListener = InputSystem.onAnyButtonPress.Subscribe(new AnyButtonObserver(this));

                if (blinkPressAnyButton)
                {
                    _blinkRoutine = StartCoroutine(BlinkRoutine());
                }

                while (!_inputReceived)
                {
                    yield return null;
                }
            }

            if (fadeOutSeconds > 0f)
            {
                float elapsed = 0f;
                while (elapsed < fadeOutSeconds)
                {
                    elapsed += Delta();
                    float t = Mathf.Clamp01(elapsed / fadeOutSeconds);
                    instructionsGroup.alpha = 1f - t;
                    yield return null;
                }
            }

            instructionsGroup.alpha = 0f;
            instructionsGroup.gameObject.SetActive(false);

            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }

            RestorePressAnyButtonUI();

            _anyButtonPressListener?.Dispose();
            _anyButtonPressListener = null;

            UnlockPlayer();
            _routine = null;
        }

        private void LockPlayer()
        {
            if (movementGate != null)
            {
                _wasLocked = !movementGate.characterCanMove;
                movementGate.SetMovementState(false);
            }

            if (movementController != null)
            {
                movementController.SetDirectionalInput(0f);
            }

            if (freezeRigidbody && playerBody != null)
            {
                _bodyWasSimulated = playerBody.simulated;
                playerBody.linearVelocity = Vector2.zero;
                playerBody.simulated = false;
            }
        }

        private void UnlockPlayer()
        {
            if (freezeRigidbody && playerBody != null)
            {
                playerBody.simulated = _bodyWasSimulated;
                playerBody.linearVelocity = Vector2.zero;
            }

            if (movementController != null)
            {
                movementController.SetDirectionalInput(0f);
            }

            if (movementGate != null && !_wasLocked)
            {
                movementGate.SetMovementState(true);
            }
        }

        private void CleanupAndUnlock()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }

            RestorePressAnyButtonUI();

            _anyButtonPressListener?.Dispose();
            _anyButtonPressListener = null;

            UnlockPlayer();
        }

        private void PreparePressAnyButtonUI()
        {
            if (dismissMode != DismissMode.PressAnyButton)
            {
                return;
            }

            if (pressAnyButtonObject != null)
            {
                _pressAnyWasActive = pressAnyButtonObject.activeSelf;
                pressAnyButtonObject.SetActive(true);
            }

            if (pressAnyButtonCanvasGroup == null && pressAnyButtonObject != null)
            {
                pressAnyButtonCanvasGroup = pressAnyButtonObject.GetComponent<CanvasGroup>();
            }

            if (pressAnyButtonGraphic == null && pressAnyButtonObject != null)
            {
                pressAnyButtonGraphic = pressAnyButtonObject.GetComponent<Graphic>();
            }

            if (pressAnyButtonCanvasGroup != null)
            {
                _pressAnyCanvasWasActive = pressAnyButtonCanvasGroup.gameObject.activeSelf;
                _pressAnyCanvasOriginalAlpha = pressAnyButtonCanvasGroup.alpha;
                pressAnyButtonCanvasGroup.alpha = blinkMaxAlpha;
                pressAnyButtonCanvasGroup.interactable = false;
                pressAnyButtonCanvasGroup.blocksRaycasts = false;
            }

            if (pressAnyButtonGraphic != null)
            {
                _pressAnyGraphicOriginalColor = pressAnyButtonGraphic.color;
                var c = _pressAnyGraphicOriginalColor;
                c.a = Mathf.Clamp01(blinkMaxAlpha);
                pressAnyButtonGraphic.color = c;
            }
        }

        private void RestorePressAnyButtonUI()
        {
            if (pressAnyButtonCanvasGroup != null)
            {
                pressAnyButtonCanvasGroup.alpha = _pressAnyCanvasOriginalAlpha;
                if (!_pressAnyCanvasWasActive)
                {
                    pressAnyButtonCanvasGroup.gameObject.SetActive(false);
                }
            }

            if (pressAnyButtonGraphic != null)
            {
                pressAnyButtonGraphic.color = _pressAnyGraphicOriginalColor;
            }

            if (pressAnyButtonObject != null && !_pressAnyWasActive)
            {
                pressAnyButtonObject.SetActive(false);
            }
        }

        private IEnumerator BlinkRoutine()
        {
            float t = 0f;
            while (!_inputReceived)
            {
                t += Delta();
                float phase = Mathf.Repeat(t, Mathf.Max(0.05f, blinkPeriodSeconds)) / Mathf.Max(0.05f, blinkPeriodSeconds);
                float wave = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
                float a = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, wave);

                if (pressAnyButtonCanvasGroup != null)
                {
                    pressAnyButtonCanvasGroup.alpha = a;
                }
                else if (pressAnyButtonGraphic != null)
                {
                    var c = pressAnyButtonGraphic.color;
                    c.a = a;
                    pressAnyButtonGraphic.color = c;
                }

                yield return null;
            }
        }

        private float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}

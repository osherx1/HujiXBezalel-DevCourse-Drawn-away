using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Prototype1
{
    /// <summary>
    /// Shows a UI instructions panel at level start, then fades it out.
    /// While visible (including fade), player movement is locked.
    /// </summary>
    public class LevelIntroInstructionsUI : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("CanvasGroup controlling the instructions panel.")]
        [SerializeField] private CanvasGroup instructionsGroup;

        [Header("Timing")]
        [Min(0f)]
        [SerializeField] private float showSeconds = 5.5f;

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
        private bool _bodyWasSimulated;
        private bool _wasLocked;

        private void Reset()
        {
            if (instructionsGroup == null)
            {
                instructionsGroup = GetComponentInChildren<CanvasGroup>(true);
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

            LockPlayer();

            _routine = StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
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

            UnlockPlayer();
        }

        private float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}

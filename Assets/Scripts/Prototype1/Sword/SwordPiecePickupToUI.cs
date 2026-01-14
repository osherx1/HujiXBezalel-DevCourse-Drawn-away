using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Prototype1
{
    /// <summary>
    /// World pickup for a sword piece.
    /// On player trigger: optionally locks movement, animates a visual flying to a UI slot, then registers the piece in SwordProgressManager.
    /// Designed to mirror the behavior/style of BrushPickupToUI, but specialized for sword pieces + staged UI.
    /// </summary>
    public class SwordPiecePickupToUI : MonoBehaviour
    {
        [Header("Piece")]
        [Tooltip("Unique ID for this piece (e.g. 0,1,2...).")]
        [SerializeField] private int pieceId;

        [Tooltip("If true, player must have sword collection unlocked before this pickup works.")]
        [SerializeField] private bool requireUnlockedToCollect = true;

        [Tooltip("If true, disables this pickup immediately if the piece was already collected.")]
        [SerializeField] private bool hideIfAlreadyCollectedOnStart = true;

        [Header("Trigger")]
        [SerializeField] private Collider2D triggerCollider;
        [Tooltip("Optional: if set, only this transform can pick up.")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("Optional: if playerTransform is null, this tag is used.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Player Lock (Optional)")]
        [SerializeField] private movementLimiter moveLimiter;
        [SerializeField] private Rigidbody2D playerRigidbody;
        [SerializeField] private characterMovement movementController;
        [SerializeField] private characterJump jumpController;

        [Tooltip("If true, immediately zero the player's velocity when pickup starts.")]
        [SerializeField] private bool zeroPlayerVelocityOnPickup = true;

        [Tooltip("If true, keeps forcing velocity to zero while the player is locked.")]
        [SerializeField] private bool keepPlayerVelocityZeroWhileLocked = true;

        [Tooltip("If true, temporarily disables characterMovement/characterJump scripts during the lock.")]
        [SerializeField] private bool disablePlayerControllersWhileLocked = true;

        [Tooltip("How long (real seconds) the player stays locked during the pickup sequence.")]
        [SerializeField, Min(0f)] private float lockDurationSeconds = 0.6f;

        [Header("World Visuals")]
        [SerializeField] private SpriteRenderer worldSprite;
        [Tooltip("Optional: glow object around the pickup (will be disabled on pickup).")]
        [SerializeField] private GameObject glowObject;

        [Header("UI Animation")]
        [SerializeField] private Canvas targetCanvas;
        [Tooltip("UI slot (RectTransform) where the piece should fly to.")]
        [SerializeField] private RectTransform uiTargetSlot;

        [Tooltip("Prefab used as the flying visual. Can be a UI object (with RectTransform) or a world object (e.g., SpriteRenderer).")]
        [FormerlySerializedAs("flyingIconPrefab")]
        [SerializeField] private GameObject flyingVisualPrefab;

        [SerializeField, Min(0.01f)] private float flyDuration = 0.6f;

        [Tooltip("Optional delay (real seconds) before the flying animation starts.")]
        [SerializeField, Min(0f)] private float delayBeforeFlySeconds;

        [Tooltip("Optional delay (real seconds) after the flying animation ends.")]
        [SerializeField, Min(0f)] private float delayAfterFlySeconds;

        [Tooltip("Curve used for flight interpolation (0..1 -> 0..1).")]
        [SerializeField] private AnimationCurve flyEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("On Arrival")]
        [Tooltip("Optional: activate this UI GameObject when the flying piece arrives.")]
        [SerializeField] private GameObject uiObjectToActivateOnArrival;

        [Tooltip("Optional: refresh this SwordUIController on arrival.")]
        [SerializeField] private SwordUIController uiControllerToRefresh;

        [Header("Progress")]
        [Tooltip("Optional: assign progress manager explicitly. If empty, uses SwordProgressManager.Instance.")]
        [SerializeField] private SwordProgressManager progress;

        [Header("Events")]
        public UnityEvent onPickupStarted;
        public UnityEvent onArrivedAtUi;
        public UnityEvent onPickupFinished;

        private bool _pickedUp;
        private bool _wasMovementEnabled;
        private bool _wasJumpEnabled;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            worldSprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (worldSprite == null)
            {
                worldSprite = GetComponentInChildren<SpriteRenderer>();
            }

            if (progress == null)
            {
                progress = SwordProgressManager.Instance;
            }

            if (hideIfAlreadyCollectedOnStart && progress != null && progress.HasPiece(pieceId))
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_pickedUp)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            if (progress == null)
            {
                progress = SwordProgressManager.Instance;
            }

            if (progress == null)
            {
                return;
            }

            if (requireUnlockedToCollect && !progress.IsUnlocked)
            {
                return;
            }

            if (progress.HasPiece(pieceId))
            {
                gameObject.SetActive(false);
                return;
            }

            // Cache player refs on first valid pickup.
            if (playerTransform == null)
            {
                playerTransform = other.transform;
            }

            if (moveLimiter == null)
            {
                moveLimiter = other.GetComponentInParent<movementLimiter>();
                if (moveLimiter == null)
                {
                    moveLimiter = FindObjectOfType<movementLimiter>(true);
                }
            }

            if (playerRigidbody == null)
            {
                playerRigidbody = other.GetComponentInParent<Rigidbody2D>();
            }

            if (movementController == null)
            {
                movementController = other.GetComponentInParent<characterMovement>();
            }

            if (jumpController == null)
            {
                jumpController = other.GetComponentInParent<characterJump>();
            }

            _pickedUp = true;
            StartCoroutine(PickupRoutine());
        }

        private bool IsPlayer(Collider2D other)
        {
            if (playerTransform != null)
            {
                return other.transform == playerTransform;
            }

            return !string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag);
        }

        private IEnumerator PickupRoutine()
        {
            float startTime = Time.unscaledTime;
            onPickupStarted?.Invoke();

            if (movementController != null)
            {
                movementController.SetDirectionalInput(0f);
            }

            if (jumpController != null)
            {
                jumpController.StopJumpInput();
            }

            if (moveLimiter != null)
            {
                moveLimiter.LockMovement();
            }

            if (playerRigidbody != null && zeroPlayerVelocityOnPickup)
            {
                playerRigidbody.linearVelocity = Vector2.zero;
            }

            if (disablePlayerControllersWhileLocked)
            {
                if (movementController != null)
                {
                    _wasMovementEnabled = movementController.enabled;
                    movementController.enabled = false;
                }

                if (jumpController != null)
                {
                    _wasJumpEnabled = jumpController.enabled;
                    jumpController.enabled = false;
                }
            }

            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            if (glowObject != null)
            {
                glowObject.SetActive(false);
            }

            if (delayBeforeFlySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delayBeforeFlySeconds);
            }

            // If we don't have UI refs, still collect and hide.
            if (targetCanvas == null || uiTargetSlot == null || flyingVisualPrefab == null)
            {
                if (worldSprite != null) worldSprite.enabled = false;
                CollectNow();
                yield return WaitRemainingLockTime(startTime);
                FinishAndCleanup();
                yield break;
            }

            // Create flying visual at the pickup's current position.
            GameObject flyingVisual = Instantiate(flyingVisualPrefab);
            flyingVisual.SetActive(true);

            // Try to copy sprite from world pickup to the flying visual.
            if (worldSprite != null)
            {
                ApplySpriteToVisual(flyingVisual, worldSprite.sprite);
                worldSprite.enabled = false;
            }

            RectTransform visualRect = flyingVisual.GetComponent<RectTransform>();
            bool isUiVisual = visualRect != null;

            Camera canvasCam = GetCanvasCamera(targetCanvas);
            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, uiTargetSlot.position);

            if (isUiVisual)
            {
                flyingVisual.transform.SetParent(targetCanvas.transform, worldPositionStays: false);
                RectTransform canvasRect = (RectTransform)targetCanvas.transform;

                Vector2 startAnchored = WorldToCanvasAnchoredPosition(transform.position, targetCanvas, canvasRect);
                Vector2 endAnchored = ScreenToCanvasAnchoredPosition(endScreen, targetCanvas, canvasRect);
                visualRect.anchoredPosition = startAnchored;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, flyDuration);
                    float eased = flyEase != null ? flyEase.Evaluate(Mathf.Clamp01(t)) : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    visualRect.anchoredPosition = Vector2.LerpUnclamped(startAnchored, endAnchored, eased);

                    if (playerRigidbody != null && keepPlayerVelocityZeroWhileLocked)
                    {
                        playerRigidbody.linearVelocity = Vector2.zero;
                    }
                    yield return null;
                }

                visualRect.anchoredPosition = endAnchored;
            }
            else
            {
                Camera worldCam = Camera.main;
                if (worldCam == null)
                {
                    Destroy(flyingVisual);
                    CollectNow();
                    FinishAndCleanup();
                    yield break;
                }

                Vector3 startWorld = transform.position;
                float depth = Mathf.Abs(worldCam.transform.position.z - startWorld.z);
                Vector3 endWorld = worldCam.ScreenToWorldPoint(new Vector3(endScreen.x, endScreen.y, depth));
                endWorld.z = startWorld.z;

                flyingVisual.transform.position = startWorld;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, flyDuration);
                    float eased = flyEase != null ? flyEase.Evaluate(Mathf.Clamp01(t)) : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    flyingVisual.transform.position = Vector3.LerpUnclamped(startWorld, endWorld, eased);

                    if (playerRigidbody != null && keepPlayerVelocityZeroWhileLocked)
                    {
                        playerRigidbody.linearVelocity = Vector2.zero;
                    }
                    yield return null;
                }

                flyingVisual.transform.position = endWorld;
            }

            if (delayAfterFlySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delayAfterFlySeconds);
            }

            Destroy(flyingVisual);

            onArrivedAtUi?.Invoke();

            if (uiObjectToActivateOnArrival != null)
            {
                uiObjectToActivateOnArrival.SetActive(true);
            }

            CollectNow();

            if (uiControllerToRefresh != null)
            {
                uiControllerToRefresh.Refresh();
            }

            yield return WaitRemainingLockTime(startTime);
            FinishAndCleanup();
        }

        private void CollectNow()
        {
            if (progress == null)
            {
                progress = SwordProgressManager.Instance;
            }

            if (progress == null)
            {
                return;
            }

            progress.TryCollectPiece(pieceId);
        }

        private IEnumerator WaitRemainingLockTime(float startUnscaledTime)
        {
            if (lockDurationSeconds <= 0f)
            {
                yield break;
            }

            float elapsed = Time.unscaledTime - startUnscaledTime;
            float remaining = lockDurationSeconds - elapsed;
            while (remaining > 0f)
            {
                if (playerRigidbody != null && keepPlayerVelocityZeroWhileLocked)
                {
                    playerRigidbody.linearVelocity = Vector2.zero;
                }

                float step = Mathf.Min(remaining, 0.05f);
                yield return new WaitForSecondsRealtime(step);
                elapsed = Time.unscaledTime - startUnscaledTime;
                remaining = lockDurationSeconds - elapsed;
            }
        }

        private void FinishAndCleanup()
        {
            if (disablePlayerControllersWhileLocked)
            {
                if (movementController != null)
                {
                    movementController.enabled = _wasMovementEnabled;
                }

                if (jumpController != null)
                {
                    jumpController.enabled = _wasJumpEnabled;
                }
            }

            if (moveLimiter != null)
            {
                moveLimiter.UnlockMovement();
            }

            onPickupFinished?.Invoke();

            gameObject.SetActive(false);
        }

        private static void ApplySpriteToVisual(GameObject visual, Sprite sprite)
        {
            if (visual == null || sprite == null)
            {
                return;
            }

            Image img = visual.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                img.enabled = true;
                return;
            }

            SpriteRenderer sr = visual.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.enabled = true;
            }
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return Camera.main;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private static Vector2 WorldToCanvasAnchoredPosition(Vector3 worldPosition, Canvas canvas, RectTransform canvasRect)
        {
            Camera cam = GetCanvasCamera(canvas);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
            return ScreenToCanvasAnchoredPosition(screenPoint, canvas, canvasRect);
        }

        private static Vector2 ScreenToCanvasAnchoredPosition(Vector2 screenPoint, Canvas canvas, RectTransform canvasRect)
        {
            Camera cam = GetCanvasCamera(canvas);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint);
            return localPoint;
        }
    }
}

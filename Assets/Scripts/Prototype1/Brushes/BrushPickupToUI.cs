using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Drawing.Buttons;

/// <summary>
/// World pickup that, on player trigger, locks movement, animates an icon into a UI slot,
/// optionally unlocks a UI button, activates a UI object, plays a spark effect, and then unlocks movement.
/// Uses unscaled time so it works even if the menu pauses time.
/// </summary>
public class BrushPickupToUI : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider2D triggerCollider;
    [Tooltip("Optional: if set, only this transform can pick up.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Optional: if playerTransform is null, this tag is used.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Player Lock")]
    [SerializeField] private movementLimiter moveLimiter;

    [Tooltip("Optional. If empty, will try to find a Rigidbody2D on the player on pickup.")]
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Tooltip("Optional. If empty, will try to find characterMovement on the player on pickup.")]
    [SerializeField] private characterMovement movementController;

    [Tooltip("Optional. If empty, will try to find characterJump on the player on pickup.")]
    [SerializeField] private characterJump jumpController;

    [Tooltip("If true, immediately zero the player's velocity when pickup starts.")]
    [SerializeField] private bool zeroPlayerVelocityOnPickup = true;

    [Tooltip("If true, keeps forcing velocity to zero while the player is locked.")]
    [SerializeField] private bool keepPlayerVelocityZeroWhileLocked = true;

    [Tooltip("If true, temporarily disables characterMovement/characterJump scripts during the lock.")]
    [SerializeField] private bool disablePlayerControllersWhileLocked = true;

    [Tooltip("If true, temporarily sets Rigidbody2D.simulated=false during the lock (hard freeze).")]
    [SerializeField] private bool freezeRigidbodySimulationWhileLocked;

    [Tooltip("How long (real seconds) the player stays locked during the pickup sequence.")]
    [SerializeField, Min(0f)] private float lockDurationSeconds = 5f;

    [Header("Bar / Menu")]
    [Tooltip("Optional: MenuController that opens the bar (e.g., on E).")]
    [SerializeField] private MenuController menuController;
    [SerializeField] private bool openMenuOnPickup = true;
    [SerializeField] private bool closeMenuOnFinish;

    [Header("World Visuals")]
    [SerializeField] private SpriteRenderer worldSprite;
    [Tooltip("Optional: glow object around the pickup (will be disabled on pickup).")]
    [SerializeField] private GameObject glowObject;

    [Header("UI Animation")]
    [SerializeField] private Canvas targetCanvas;
    [Tooltip("UI slot (RectTransform) where the brush should fly to.")]
    [SerializeField] private RectTransform uiTargetSlot;
    [Tooltip("Prefab used as the flying visual. Can be a UI object (with RectTransform) or a world object (e.g., SpriteRenderer).")]
    [FormerlySerializedAs("flyingIconPrefab")]
    [SerializeField] private GameObject flyingVisualPrefab;
    [SerializeField, Min(0.01f)] private float flyDuration = 0.6f;

    [Tooltip("Optional delay (real seconds) before the flying animation starts.")]
    [SerializeField, Min(0f)] private float delayBeforeFlySeconds;

    [Tooltip("Optional delay (real seconds) after the flying animation ends (before unlock/finish actions).")]
    [SerializeField, Min(0f)] private float delayAfterFlySeconds;

    [Tooltip("Curve used for flight interpolation (0..1 -> 0..1).")]
    [SerializeField] private AnimationCurve flyEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Unlock / Activate")]
    [Tooltip("If assigned, will set Button.interactable = true (and SetActive(true) on its GameObject).")]
    [SerializeField] private Button buttonToUnlock;
    [Tooltip("If assigned, this UI object will be activated at the end (e.g., the brush icon in the bar).")]
    [SerializeField] private GameObject uiObjectToActivate;

    [Header("Effects")]
    [Tooltip("Optional particle system prefab to spawn when the icon reaches the UI slot.")]
    [SerializeField] private ParticleSystem sparkEffectPrefab;

    [Tooltip("Optional: existing ParticleSystem in the scene to play (instead of instantiating a prefab).")]
    [SerializeField] private ParticleSystem sparkEffectInWorld;

    [Tooltip("World Z plane for spawning the spark when converting from UI screen position (2D usually uses 0).")]
    [SerializeField] private float sparkWorldZ = 0f;

    [Tooltip("Optional UI prefab (RectTransform) to spawn on the canvas at the UI slot when the icon arrives. Useful if world particles are not visible.")]
    [SerializeField] private GameObject uiSparkPrefab;

    [SerializeField, Min(0.01f)] private float uiSparkLifetimeSeconds = 0.6f;

    [Header("Events")]
    public UnityEvent onPickupStarted;
    public UnityEvent onPickupFinished;

    private bool _pickedUp;
    private bool _wasMovementEnabled;
    private bool _wasJumpEnabled;
    private bool _bodyWasSimulated;

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

        if (menuController == null)
        {
            menuController = FindObjectOfType<MenuController>(true);
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

        if (freezeRigidbodySimulationWhileLocked && playerRigidbody != null)
        {
            _bodyWasSimulated = playerRigidbody.simulated;
            playerRigidbody.simulated = false;
        }

        if (menuController != null && openMenuOnPickup)
        {
            menuController.OpenMenu();
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

        // If we don't have UI refs, still perform unlock/finish and hide the pickup.
        if (targetCanvas == null || uiTargetSlot == null || flyingVisualPrefab == null)
        {
            if (worldSprite != null) worldSprite.enabled = false;
            ApplyUnlocks();
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

        // Determine if the flying prefab is UI (RectTransform) or world-space.
        RectTransform visualRect = flyingVisual.GetComponent<RectTransform>();
        bool isUiVisual = visualRect != null;

        Camera canvasCam = GetCanvasCamera(targetCanvas);
        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, uiTargetSlot.position);

        if (isUiVisual)
        {
            // Parent under the canvas so anchoredPosition makes sense.
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
            // World-space visual: move in world so that its screen position matches the UI slot.
            Camera worldCam = Camera.main;
            if (worldCam == null)
            {
                // No camera; just skip animation and finish.
                Destroy(flyingVisual);
                ApplyUnlocks();
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

        if (sparkEffectPrefab != null)
        {
            SpawnSparkAtUiSlot();
        }
        else if (uiSparkPrefab != null)
        {
            SpawnUiSparkAtSlot();
        }

        if (delayAfterFlySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delayAfterFlySeconds);
        }

        Destroy(flyingVisual);

        ApplyUnlocks();
        yield return WaitRemainingLockTime(startTime);
        FinishAndCleanup();
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

    private void ApplyUnlocks()
    {
        if (buttonToUnlock != null)
        {
            if (!buttonToUnlock.gameObject.activeSelf)
            {
                buttonToUnlock.gameObject.SetActive(true);
            }

            buttonToUnlock.interactable = true;
        }

        if (uiObjectToActivate != null)
        {
            uiObjectToActivate.SetActive(true);
        }
    }

    private void FinishAndCleanup()
    {
        if (freezeRigidbodySimulationWhileLocked && playerRigidbody != null)
        {
            playerRigidbody.simulated = _bodyWasSimulated;
        }

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

        if (menuController != null && closeMenuOnFinish)
        {
            menuController.CloseMenu();
        }

        onPickupFinished?.Invoke();

        gameObject.SetActive(false);
    }

    private void SpawnSparkAtUiSlot()
    {
        if ((sparkEffectPrefab == null && sparkEffectInWorld == null) || uiTargetSlot == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, uiTargetSlot.position);
        float depth = Mathf.Abs(cam.transform.position.z - sparkWorldZ);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        worldPos.z = sparkWorldZ;

        if (sparkEffectInWorld != null)
        {
            sparkEffectInWorld.transform.position = worldPos;
            if (!sparkEffectInWorld.gameObject.activeSelf)
            {
                sparkEffectInWorld.gameObject.SetActive(true);
            }
            sparkEffectInWorld.Play(true);
            return;
        }

        ParticleSystem ps = Instantiate(sparkEffectPrefab, worldPos, Quaternion.identity);
        ps.gameObject.SetActive(true);
        ps.Play(true);
        Destroy(ps.gameObject, Mathf.Max(0.5f, ps.main.duration + ps.main.startLifetime.constantMax));
    }

    private void SpawnUiSparkAtSlot()
    {
        if (uiSparkPrefab == null || targetCanvas == null || uiTargetSlot == null)
        {
            return;
        }

        RectTransform canvasRect = targetCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        // If this prefab is UI (RectTransform), spawn it under the canvas.
        // If it's a world prefab (e.g., ParticleSystem), spawn it in world at the UI slot screen position.
        if (uiSparkPrefab.GetComponent<RectTransform>() != null)
        {
            GameObject spark = Instantiate(uiSparkPrefab, targetCanvas.transform);
            spark.SetActive(true);

            RectTransform sparkRect = spark.GetComponent<RectTransform>();
            if (sparkRect != null)
            {
                Camera canvasCam = GetCanvasCamera(targetCanvas);
                Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, uiTargetSlot.position);
                sparkRect.anchoredPosition = ScreenToCanvasAnchoredPosition(endScreen, targetCanvas, canvasRect);
            }

            Destroy(spark, uiSparkLifetimeSeconds);
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, uiTargetSlot.position);
        float depth = Mathf.Abs(cam.transform.position.z - sparkWorldZ);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        worldPos.z = sparkWorldZ;

        GameObject worldSpark = Instantiate(uiSparkPrefab, worldPos, Quaternion.identity);
        worldSpark.SetActive(true);
        ParticleSystem ps = worldSpark.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Play(true);
        }

        Destroy(worldSpark, uiSparkLifetimeSeconds);
    }

    private static void ApplySpriteToVisual(GameObject visual, Sprite sprite)
    {
        if (visual == null || sprite == null)
        {
            return;
        }

        // UI Image
        Image img = visual.GetComponentInChildren<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.enabled = true;
            return;
        }

        // World SpriteRenderer
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

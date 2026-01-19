using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Drawing.Buttons;
using Drawing;

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

    [Header("Input Lock (E)")]
    [Tooltip("If true, temporarily disables MenuInputListener (E toggle) during the pickup sequence.")]
    [SerializeField] private bool disableMenuToggleWhileLocked = true;

    [Tooltip("Optional: assign the MenuInputListener that listens to E. If empty, will try to find one.")]
    [SerializeField] private MenuInputListener menuInputListener;

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

    [Header("Auto Select (Use Immediately)")]
    [Tooltip("If true, the collected brush/tool will be selected as the active tool automatically (no need to open the PopBar and click it).")]
    [SerializeField] private bool autoSelectUnlockedToolOnPickup = true;

    [Tooltip("Optional: explicitly assign the DrawingConfigButton to select. If empty, will try to find it on/under buttonToUnlock.")]
    [SerializeField] private DrawingConfigButton toolConfigToAutoSelect;

    [Header("Effects")]
    [Tooltip("Optional: ParticleSystem that is attached to this pickup (or a child). It will be detached and played so it won't turn off when the pickup is deactivated.")]
    [SerializeField] private ParticleSystem sparkDetachFromPickup;

    [Tooltip("Optional: existing ParticleSystem in the scene to move + play.")]
    [SerializeField] private ParticleSystem sparkEffectInWorld;

    [Tooltip("Optional: prefab ParticleSystem to spawn in the world and auto-destroy.")]
    [SerializeField] private ParticleSystem sparkEffectPrefab;

    [Tooltip("World Z plane for playing/spawning the spark when converting from UI screen position (2D usually uses 0).")]
    [SerializeField] private float sparkWorldZ = 0f;

    [Tooltip("How many real seconds the spark should last. 0 = use the particle system's own duration/lifetime.")]
    [SerializeField, Min(0f)] private float sparkLifetimeSecondsOverride = 0f;

    [Header("Events")]
    public UnityEvent onPickupStarted;
    public UnityEvent onPickupFinished;

    private bool _pickedUp;
    private bool _wasMovementEnabled;
    private bool _wasJumpEnabled;
    private bool _bodyWasSimulated;

    private bool _menuInputWasEnabled;

    private Coroutine _stopInWorldSparkCoroutine;

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

        if (menuInputListener == null)
        {
            menuInputListener = FindObjectOfType<MenuInputListener>(true);
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

        PrepareSparkForSequence();

        DisableMenuInputIfNeeded();

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
            // TryAutoSelectUnlockedTool();
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

        PlaySparkAtUiSlotWorld();

        if (delayAfterFlySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delayAfterFlySeconds);
        }

        Destroy(flyingVisual);

        ApplyUnlocks();
        // TryAutoSelectUnlockedTool();
        yield return WaitRemainingLockTime(startTime);
        FinishAndCleanup();
    }

    private void TryAutoSelectUnlockedTool()
    {
        if (!autoSelectUnlockedToolOnPickup)
        {
            return;
        }

        DrawingConfigButton config = toolConfigToAutoSelect;

        if (config == null && buttonToUnlock != null)
        {
            // Try to find a DrawingConfigButton associated with the unlocked UI.
            config = buttonToUnlock.GetComponent<DrawingConfigButton>();
            if (config == null)
            {
                config = buttonToUnlock.GetComponentInParent<DrawingConfigButton>();
            }
            if (config == null)
            {
                config = buttonToUnlock.GetComponentInChildren<DrawingConfigButton>(true);
            }
        }

        if (config != null)
        {
            // Select without requiring the menu/PopBar to be open.
            config.SelectTool(false);
            return;
        }

        // Fallback: invoke the UI button click if no config button was found.
        if (buttonToUnlock != null)
        {
            buttonToUnlock.onClick?.Invoke();
        }
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

        RestoreMenuInputIfNeeded();

        onPickupFinished?.Invoke();

        gameObject.SetActive(false);
    }

    private void DisableMenuInputIfNeeded()
    {
        if (!disableMenuToggleWhileLocked)
        {
            return;
        }

        if (menuInputListener == null)
        {
            menuInputListener = FindObjectOfType<MenuInputListener>(true);
        }

        if (menuInputListener == null)
        {
            return;
        }

        _menuInputWasEnabled = menuInputListener.enabled;
        menuInputListener.enabled = false;
    }

    private void RestoreMenuInputIfNeeded()
    {
        if (!disableMenuToggleWhileLocked)
        {
            return;
        }

        if (menuInputListener == null)
        {
            return;
        }

        menuInputListener.enabled = _menuInputWasEnabled;

        // If the menu is hold-to-open, we may have missed the "canceled" event while disabled.
        // Sync so that if E is not currently held, the menu closes immediately.
        if (_menuInputWasEnabled)
        {
            menuInputListener.SyncMenuToHoldState();
        }
    }

    private void PlaySparkAtUiSlotWorld()
    {
        if (sparkDetachFromPickup == null && sparkEffectInWorld == null && sparkEffectPrefab == null)
        {
            return;
        }

        if (uiTargetSlot == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Camera canvasCam = targetCanvas != null ? GetCanvasCamera(targetCanvas) : null;
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(canvasCam, uiTargetSlot.position);
        float depth = Mathf.Abs(cam.transform.position.z - sparkWorldZ);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        worldPos.z = sparkWorldZ;

        // 1) If the spark is attached to the pickup, detach it so it survives after this pickup disables itself.
        if (sparkDetachFromPickup != null)
        {
            Transform t = sparkDetachFromPickup.transform;
            t.SetParent(null, worldPositionStays: true);
            t.position = worldPos;

            GameObject go = sparkDetachFromPickup.gameObject;
            if (!go.activeSelf)
            {
                go.SetActive(true);
            }

            sparkDetachFromPickup.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkDetachFromPickup.Play(true);

            float ttl = GetSparkLifetimeSeconds(sparkDetachFromPickup);
            Destroy(go, ttl);
            return;
        }

        if (sparkEffectInWorld != null)
        {
            sparkEffectInWorld.transform.position = worldPos;
            if (!sparkEffectInWorld.gameObject.activeSelf)
            {
                sparkEffectInWorld.gameObject.SetActive(true);
            }
            sparkEffectInWorld.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkEffectInWorld.Play(true);

            if (_stopInWorldSparkCoroutine != null)
            {
                StopCoroutine(_stopInWorldSparkCoroutine);
            }

            float ttl = GetSparkLifetimeSeconds(sparkEffectInWorld);
            _stopInWorldSparkCoroutine = StartCoroutine(StopSparkAfterSeconds(sparkEffectInWorld, ttl));
            return;
        }

        ParticleSystem ps = Instantiate(sparkEffectPrefab, worldPos, Quaternion.identity);
        ps.gameObject.SetActive(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(true);
        Destroy(ps.gameObject, GetSparkLifetimeSeconds(ps));
    }

    private float GetSparkLifetimeSeconds(ParticleSystem ps)
    {
        if (sparkLifetimeSecondsOverride > 0f)
        {
            return sparkLifetimeSecondsOverride;
        }

        if (ps == null)
        {
            return 0.5f;
        }

        // Best-effort: duration + max start lifetime.
        float lifetime = ps.main.duration;
        try
        {
            lifetime += ps.main.startLifetime.constantMax;
        }
        catch
        {
            // Some lifetime modules may not support constantMax in older settings; ignore.
        }

        return Mathf.Max(0.1f, lifetime);
    }

    private IEnumerator StopSparkAfterSeconds(ParticleSystem ps, float seconds)
    {
        if (ps == null)
        {
            yield break;
        }

        if (seconds > 0f)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void PrepareSparkForSequence()
    {
        // Prevent "Play On Awake" sparks from firing at the pickup's original position.
        if (sparkDetachFromPickup != null)
        {
            sparkDetachFromPickup.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Keep it inactive until we actually want to play it.
            if (sparkDetachFromPickup.gameObject.activeSelf)
            {
                sparkDetachFromPickup.gameObject.SetActive(false);
            }
        }

        if (sparkEffectInWorld != null)
        {
            sparkEffectInWorld.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Prefab doesn't exist in-scene yet, nothing to prepare.
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
            // For Screen Space - Overlay, Unity UI utility methods expect a null camera.
            return null;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using Drawing.LineControl;
using Drawing.Buttons;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//This script handles the character being killed and respawning

public class characterHurt : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Vector3 checkpointFlag;
    [Tooltip("Optional starting checkpoint for this level. If set, the player will respawn here by default.")]
    [SerializeField] private Transform initialCheckpoint;
    [SerializeField] Animator myAnim;
    [SerializeField] movementLimiter myLimit;
    [SerializeField] optionsManagement optionsScript;
    [SerializeField] AudioSource hurtSFX;
    [SerializeField] labOpener labStatus;
    private Coroutine flashRoutine;
    Rigidbody2D body;
    [SerializeField] public SpriteRenderer spriteRenderer;

    [Header("Respawn - Drawing Reset")]
    [Tooltip("Optional: reference the existing ResetBoard component (the same one used by the UI reset button). If set, death will reuse its reset logic.")]
    [SerializeField] private ResetBoard resetBoard;

    [Tooltip("If set, lines will be cleared by destroying children under this root (recommended for performance).")]
    [SerializeField] private Transform linesRoot;

    [Tooltip("If true, lines will be cleared when the player respawns.")]
    [SerializeField] private bool resetLinesOnRespawn = true;

    [Header("Respawn - Screen Overlay")]
    [Tooltip("Optional UI overlay (recommended). Assign a CanvasGroup that controls a full-screen red Image.")]
    [SerializeField] private CanvasGroup deathOverlayCanvasGroup;

    [Tooltip("Optional red overlay sprite (legacy). If both UI and sprite are assigned, UI takes priority.")]
    [SerializeField] private SpriteRenderer deathOverlay;

    [Tooltip("If true, enables the overlay when dying.")]
    [SerializeField] private bool showDeathOverlayOnDeath = true;

    [Header("Cheats")]
    [Tooltip("If enabled, Ctrl+Q instantly teleports the player to the current checkpoint (and resets lines if configured).")]
    [SerializeField] private bool enableRespawnCheat = true;

    [Header("Settings")]
    [Tooltip("Seconds to freeze the player BEFORE showing the red overlay.")]
    [SerializeField] float respawnTime;

    [Tooltip("Seconds to show the red overlay BEFORE the actual respawn happens.")]
    [SerializeField] private float overlayDuration = 0.75f;

    [Tooltip("If true, disables Rigidbody2D simulation during the death delay so the player fully stops.")]
    [SerializeField] private bool freezeRigidbodyDuringDeath = true;
    [SerializeField] private float flashDuration;

    [Header("Events")]
    [SerializeField] public UnityEvent onHurt = new UnityEvent();

    [Header("Current State")]
    bool waiting = false;
    bool hurting = false;

    private Tween _respawnTween;
    private bool _deathOverlayWasActive;
    private bool _deathOverlayCanvasWasActive;
    private float _deathOverlayCanvasOriginalAlpha;
    private bool _bodyWasSimulated;
    private bool _flashRendererOriginalEnabled;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            _flashRendererOriginalEnabled = spriteRenderer.enabled;
        }

        if (deathOverlayCanvasGroup != null)
        {
            _deathOverlayCanvasWasActive = deathOverlayCanvasGroup.gameObject.activeSelf;
            _deathOverlayCanvasOriginalAlpha = deathOverlayCanvasGroup.alpha;

            // Ensure hidden on start, even if active in hierarchy.
            deathOverlayCanvasGroup.alpha = 0f;
            deathOverlayCanvasGroup.interactable = false;
            deathOverlayCanvasGroup.blocksRaycasts = false;

            if (!_deathOverlayCanvasWasActive)
            {
                deathOverlayCanvasGroup.gameObject.SetActive(false);
            }
        }

        if (deathOverlay != null)
        {
            _deathOverlayWasActive = deathOverlay.gameObject.activeSelf;
            deathOverlay.enabled = false;
            if (!_deathOverlayWasActive)
            {
                deathOverlay.gameObject.SetActive(false);
            }
        }

        if (initialCheckpoint != null)
        {
            checkpointFlag = initialCheckpoint.position;
        }
        else if (checkpointFlag == Vector3.zero)
        {
            checkpointFlag = transform.position;
        }

    }

    private void Update()
    {
        if (!enableRespawnCheat)
        {
            return;
        }

        var kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        bool ctrlHeld = (kb.leftCtrlKey != null && kb.leftCtrlKey.isPressed)
                        || (kb.rightCtrlKey != null && kb.rightCtrlKey.isPressed);

        if (ctrlHeld && kb.qKey != null && kb.qKey.wasPressedThisFrame)
        {
            ForceRespawnToCheckpoint();
        }
    }

    public void newCheckpoint(Vector3 flagPos)
    {
        //When the player touches a checkpoint, it passes its position to this script
        checkpointFlag = flagPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //If the player hits layer 7 (saw blade) or 8 (spikes), start the hurt routine
        if (collision.gameObject.layer == 7 || collision.gameObject.layer == 8)
        {
            TriggerHazardHit(collision.gameObject.layer == 8);
        }
    }

    private bool LabBlocksRespawn()
    {
        return labStatus != null && (labStatus.labIsOpen || labStatus.labIsTransitioning);
    }

    private bool CanStartHurt()
    {
        return !LabBlocksRespawn() && !hurting;
    }

    public void TriggerHazardHit(bool zeroOutVelocity)
    {
        if (!CanStartHurt())
        {
            return;
        }

        if (_respawnTween != null && _respawnTween.IsActive())
        {
            _respawnTween.Kill(false);
            _respawnTween = null;
        }

        if (showDeathOverlayOnDeath && deathOverlay != null)
        {
            if (!deathOverlay.gameObject.activeSelf)
            {
                deathOverlay.gameObject.SetActive(true);
            }

            deathOverlay.enabled = true;
        }

        if (zeroOutVelocity && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (freezeRigidbodyDuringDeath && body != null)
        {
            _bodyWasSimulated = body.simulated;
            body.simulated = false;
        }

        hurting = true;
        hurtRoutine();
    }

    public void hurtRoutine()
    {
        myLimit.characterCanMove = false;

        if (optionsScript.screenShake)
        {
            //The screenshake is played in a Unity Event, provided the option is turned on
            onHurt?.Invoke();
        }

        hurtSFX.Play();

        Stop(0.1f);
        myAnim.SetTrigger("Hurt");
        Flash();

        // Death timeline:
        // 1) Freeze delay (respawnTime)
        // 2) Enable red overlay
        // 3) Overlay duration (overlayDuration)
        // 4) Respawn
        _respawnTween = DOTween.Sequence()
            .SetUpdate(true) // use unscaled time (still works during hit-stop / timeScale changes)
            .AppendInterval(Mathf.Max(0f, respawnTime))
            .AppendCallback(EnableDeathOverlay)
            .AppendInterval(Mathf.Max(0f, overlayDuration))
            .AppendCallback(respawnRoutine);
    }

    //These three functions handle the hit stop effect, where the game pauses for a brief moment on death
    public void Stop(float duration)
    {
        Stop(duration, 0.0f);
    }

    public void Stop(float duration, float timeScale)
    {
        if (waiting)
            return;
        Time.timeScale = timeScale;
        StartCoroutine(Wait(duration));
    }

    IEnumerator Wait(float duration)
    {
        waiting = true;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        waiting = false;
    }

    //These two functions handle the flashing white effect when Kit dies
    public void Flash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            flashRoutine = null;
            yield break;
        }

        // Show the flash
        spriteRenderer.enabled = true;

        // Pause the execution of this function for "duration" seconds.
        yield return new WaitForSeconds(flashDuration);

        // Restore to original state (prevents hiding the player's main sprite if this is the same renderer)
        spriteRenderer.enabled = _flashRendererOriginalEnabled;

        // Set the routine to null, signaling that it's finished.
        flashRoutine = null;
    }

    //After the timer ends, respawn Kit at the nearest checkpoint and let her move again
    private void respawnRoutine()
    {
        _respawnTween = null;

        if (resetLinesOnRespawn)
        {
            ResetAllLines();
        }

        if (body != null)
        {
            body.simulated = _bodyWasSimulated;
            body.linearVelocity = Vector2.zero;
        }

        transform.position = checkpointFlag;
        myLimit.characterCanMove = true;
        myAnim.SetTrigger("Okay");
        hurting = false;

        DisableDeathOverlay();
    }

    private void EnableDeathOverlay()
    {
        if (!showDeathOverlayOnDeath)
        {
            return;
        }

        if (deathOverlayCanvasGroup != null)
        {
            if (!deathOverlayCanvasGroup.gameObject.activeSelf)
            {
                deathOverlayCanvasGroup.gameObject.SetActive(true);
            }

            deathOverlayCanvasGroup.alpha = 1f;
            deathOverlayCanvasGroup.interactable = false;
            deathOverlayCanvasGroup.blocksRaycasts = false;
            return;
        }

        if (deathOverlay == null)
        {
            return;
        }

        if (!deathOverlay.gameObject.activeSelf)
        {
            deathOverlay.gameObject.SetActive(true);
        }

        deathOverlay.enabled = true;
    }

    private void DisableDeathOverlay()
    {
        if (deathOverlayCanvasGroup != null)
        {
            deathOverlayCanvasGroup.alpha = 0f;
            deathOverlayCanvasGroup.interactable = false;
            deathOverlayCanvasGroup.blocksRaycasts = false;

            if (!_deathOverlayCanvasWasActive)
            {
                deathOverlayCanvasGroup.gameObject.SetActive(false);
            }
            else
            {
                // Restore original alpha if desired; keep hidden (0) is usually better for overlay.
                // If you prefer restoring, uncomment next line.
                // deathOverlayCanvasGroup.alpha = _deathOverlayCanvasOriginalAlpha;
            }
        }

        if (deathOverlay != null)
        {
            deathOverlay.enabled = false;
            if (!_deathOverlayWasActive)
            {
                deathOverlay.gameObject.SetActive(false);
            }
        }
    }

    public void ForceRespawnToCheckpoint()
    {
        if (LabBlocksRespawn())
        {
            return;
        }

        if (_respawnTween != null && _respawnTween.IsActive())
        {
            _respawnTween.Kill(false);
            _respawnTween = null;
        }

        if (resetLinesOnRespawn)
        {
            ResetAllLines();
        }

        if (body != null)
        {
            body.simulated = _bodyWasSimulated;
            body.linearVelocity = Vector2.zero;
        }

        transform.position = checkpointFlag;
        myLimit.characterCanMove = true;
        myAnim.SetTrigger("Okay");
        hurting = false;

        DisableDeathOverlay();
    }

    private void ResetAllLines()
    {
        if (resetBoard != null)
        {
            resetBoard.ResetLines();
            return;
        }

        if (linesRoot != null)
        {
            for (int i = linesRoot.childCount - 1; i >= 0; i--)
            {
                var child = linesRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            return;
        }

        var lines = FindObjectsOfType<Line>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null)
            {
                Destroy(lines[i].gameObject);
            }
        }
    }
}
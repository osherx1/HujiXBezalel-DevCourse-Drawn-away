using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Drawing.Buttons; 
using Drawing.LineControl;

public class BossIntroTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private string playerTag = "Player";

    [Header("Camera Cutscene")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float targetOrthoSize = 18f;
    [SerializeField] private float panDuration = 2.0f;
    [SerializeField] private float holdDuration = 3.0f;
    [SerializeField] private AnimationCurve panCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Material Scattering (Animation)")]
    [SerializeField] private float scatterDuration = 1.0f;
    [SerializeField] private AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Define which materials are taken and where they go.")]
    [SerializeField] private List<ScatteredMaterial> materialsToScatter;

    [Header("Player Control (Auto-Detected)")]
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private MonoBehaviour movementController; 
    [SerializeField] private MonoBehaviour jumpController;     
    [SerializeField] private LineManager lineManager;          
    [SerializeField] private MenuController menuController;    

    [System.Serializable]
    public struct ScatteredMaterial
    {
        public Button uiButton;
        public GameObject pickupInstance;
        public Transform spawnLocation;
    }

    private bool _hasTriggered = false;
    
    // State backups
    private bool _wasMovementEnabled;
    private bool _wasJumpEnabled;
    private bool _wasDrawingEnabled;
    private bool _bodyWasSimulated;

    private void Awake()
    {
        if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        _hasTriggered = true;
        if (triggerCollider != null) triggerCollider.enabled = false;

        if (playerRigidbody == null) playerRigidbody = other.GetComponentInParent<Rigidbody2D>();
        if (movementController == null) movementController = other.GetComponentInParent<MonoBehaviour>(); 
        if (lineManager == null) lineManager = FindFirstObjectByType<LineManager>();
        if (menuController == null) menuController = FindFirstObjectByType<MenuController>();

        StartCoroutine(IntroSequenceRoutine());
    }

    private IEnumerator IntroSequenceRoutine()
    {
        // 1. LOCK
        SetPlayerLock(true);
        if (menuController != null) menuController.CloseMenu();

        // 2. PAN CAMERA OUT
        Camera cam = Camera.main;
        float originalSize = 5f;
        Vector3 originalPos = Vector3.zero;

        if (cam != null && cameraTarget != null)
        {
            originalPos = cam.transform.position;
            originalSize = cam.orthographicSize;
            Vector3 targetPos = cameraTarget.position;
            targetPos.z = originalPos.z;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / panDuration;
                float curve = panCurve.Evaluate(t);
                cam.transform.position = Vector3.Lerp(originalPos, targetPos, curve);
                cam.orthographicSize = Mathf.Lerp(originalSize, targetOrthoSize, curve);
                yield return null;
            }
        }

        // 3. SCATTER ANIMATION
        // We start the scattering routine and let it run parallel to the "Hold" wait
        StartCoroutine(ScatterMaterialsRoutine());
        
        yield return new WaitForSecondsRealtime(holdDuration);

        // 4. PAN CAMERA BACK
        if (cam != null && playerRigidbody != null)
        {
            Vector3 startPos = cam.transform.position;
            float startSize = cam.orthographicSize;
            Vector3 returnPos = playerRigidbody.transform.position;
            returnPos.z = originalPos.z;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / panDuration;
                float curve = panCurve.Evaluate(t);
                cam.transform.position = Vector3.Lerp(startPos, returnPos, curve);
                cam.orthographicSize = Mathf.Lerp(startSize, originalSize, curve);
                yield return null;
            }
        }

        // 5. UNLOCK
        SetPlayerLock(false);
        gameObject.SetActive(false);
    }

    private IEnumerator ScatterMaterialsRoutine()
    {
        // The start position for all items is the Player (simulating them dropping from inventory)
        Vector3 origin = playerRigidbody != null ? playerRigidbody.transform.position : transform.position;

        foreach (var item in materialsToScatter)
        {
            // Lock UI
            if (item.uiButton != null) item.uiButton.interactable = false;

            // Activate and Animate Pickup
            if (item.pickupInstance != null && item.spawnLocation != null)
            {
                // Start at player
                item.pickupInstance.transform.position = origin;
                item.pickupInstance.SetActive(true);

                // Start individual flight coroutine
                StartCoroutine(MoveToTarget(item.pickupInstance.transform, item.spawnLocation.position));
            }
        }
        yield return null;
    }

    private IEnumerator MoveToTarget(Transform obj, Vector3 targetPos)
    {
        Vector3 startPos = obj.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / scatterDuration;
            float curve = scatterCurve.Evaluate(t);
            obj.position = Vector3.Lerp(startPos, targetPos, curve);
            yield return null;
        }
        obj.position = targetPos;
    }

    private void SetPlayerLock(bool isLocked)
    {
        if (isLocked)
        {
            if (movementController != null) _wasMovementEnabled = movementController.enabled;
            if (jumpController != null) _wasJumpEnabled = jumpController.enabled;
            if (lineManager != null) _wasDrawingEnabled = lineManager.enabled;
            if (playerRigidbody != null) _bodyWasSimulated = playerRigidbody.simulated;

            if (movementController != null) movementController.enabled = false;
            if (jumpController != null) jumpController.enabled = false;
            if (lineManager != null) lineManager.enabled = false;
            
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.simulated = false; 
            }
        }
        else
        {
            if (movementController != null) movementController.enabled = _wasMovementEnabled;
            if (jumpController != null) jumpController.enabled = _wasJumpEnabled;
            if (lineManager != null) lineManager.enabled = _wasDrawingEnabled;
            
            if (playerRigidbody != null)
            {
                playerRigidbody.simulated = _bodyWasSimulated;
            }
        }
    }
}
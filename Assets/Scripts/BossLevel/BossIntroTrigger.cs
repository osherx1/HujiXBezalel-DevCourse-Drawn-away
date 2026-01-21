using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Drawing.Buttons;
using Drawing.LineControl;

public class BossIntroCinemachineTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private string playerTag = "Player";

    [Header("Cinemachine Integration")]
    [Tooltip("Drag the Virtual Camera GameObject that focuses on the Giant here. Make sure it is DISABLED initially.")]
    [SerializeField] private GameObject giantVirtualCamera;
    [Tooltip("How long Cinemachine takes to blend (match this to your Cinemachine Brain 'Default Blend' time).")]
    [SerializeField] private float blendDuration = 2.0f;
    [Tooltip("How long to stay focused on the giant.")]
    [SerializeField] private float holdDuration = 3.0f;

    [Header("Material Scattering")]
    [SerializeField] private float scatterDuration = 1.0f;
    [SerializeField] private AnimationCurve scatterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private List<ScatteredMaterial> materialsToScatter;

    [Header("Player Control")]
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
        
        // Ensure the boss cam is off at start
        if (giantVirtualCamera != null) giantVirtualCamera.SetActive(false);
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
        // 1. LOCK PLAYER
        SetPlayerLock(true);
        if (menuController != null) menuController.CloseMenu();

        // 2. SWITCH CAMERA (Pan to Giant)
        // Enabling the high-priority camera makes Cinemachine blend to it automatically
        if (giantVirtualCamera != null)
        {
            giantVirtualCamera.SetActive(true);
        }
        
        // Wait for the blend to finish so we are fully looking at the giant
        yield return new WaitForSecondsRealtime(blendDuration);

        // 3. SCATTER ANIMATION
        // Start scattering materials while holding the view
        StartCoroutine(ScatterMaterialsRoutine());
        
        yield return new WaitForSecondsRealtime(holdDuration);

        // 4. SWITCH CAMERA BACK (Pan to Player)
        // Disabling the boss camera makes Cinemachine fall back to the Player camera
        if (giantVirtualCamera != null)
        {
            giantVirtualCamera.SetActive(false);
        }
        
        // Wait for the blend back
        yield return new WaitForSecondsRealtime(blendDuration);

        // 5. UNLOCK
        SetPlayerLock(false);
        gameObject.SetActive(false);
    }

    private IEnumerator ScatterMaterialsRoutine()
    {
        Vector3 origin = playerRigidbody != null ? playerRigidbody.transform.position : transform.position;

        foreach (var item in materialsToScatter)
        {
            if (item.uiButton != null) item.uiButton.interactable = false;

            if (item.pickupInstance != null && item.spawnLocation != null)
            {
                // Start from player position
                item.pickupInstance.transform.position = origin;
                item.pickupInstance.SetActive(true);
                
                // Fly to destination
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
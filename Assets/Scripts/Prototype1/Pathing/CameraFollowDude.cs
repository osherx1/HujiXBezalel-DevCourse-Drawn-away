using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollowDude : MonoBehaviour
{
    [SerializeField] private Transform dude;
    [SerializeField] private Transform player;
    [SerializeField] private CinemachineCamera camera;
    
    [Header("Player Lock")]
    [SerializeField] private movementLimiter movementGate;
    [SerializeField] private characterMovement movementController;
    [Tooltip("Optional: also freeze Rigidbody2D simulation during the intro.")]
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private bool freezeRigidbody = true;
    
    [Header("Cursor Lock")]
    [SerializeField] private bool lockCursor = true;
    
    [Header("Line Control")]
    [SerializeField] private MonoBehaviour lineManager; // Drag your LineManager script here

    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    
    private bool _bodyWasSimulated;
    private bool _wasLocked;
    private bool _didCrossTrigger;
    
    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_didCrossTrigger)
        {
            camera.Follow = dude;
            LockPlayer();
        }
        else if (other.CompareTag("Dude"))
        {
            Debug.Log("Dude entered trigger");
            _didCrossTrigger = true;
            camera.Follow = player;
            UnlockPlayer();
        }
    }
    
    private void LockPlayer()
    {
        if (lineManager != null)
        {
            lineManager.enabled = false;
        }
        
        if (movementGate != null)
        {
            _wasLocked = !movementGate.characterCanMove;
            movementGate.SetMovementState(false);
        }

        if (movementController != null)
        {
            movementController.SetDirectionalInput(0f);
        }

        if (playerBody != null)
        {
            freezeRigidbody = true;
            _bodyWasSimulated = playerBody.simulated;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }
        
        if (lockCursor)
        {
            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UnlockPlayer()
    {
        if (lineManager != null)
        {
            lineManager.enabled = true;
        }
        
        if (freezeRigidbody && playerBody != null)
        {
            playerBody.simulated = true;
            playerBody.linearVelocity = Vector2.zero;
            freezeRigidbody = false;
        }

        if (movementController != null)
        {
            movementController.SetDirectionalInput(0f);
        }

        if (movementGate != null && !_wasLocked)
        {
            movementGate.SetMovementState(true);
        }
        
        if (lockCursor)
        {
            Cursor.lockState = _previousLockMode;
            Cursor.visible = _previousCursorVisible;
        }
    }
}

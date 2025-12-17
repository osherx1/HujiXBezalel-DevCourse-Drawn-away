using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine; // IMPORTANT: Namespace for Cinemachine 3.x (Use 'Cinemachine' for older versions)

public class HeavyDoorButton : MonoBehaviour
{
    [Header("Assignments")]
    public Transform doorObject;
    public Transform buttonPlunger; 
    
    [Header("Door Settings")]
    public float openDistance = 3f;
    public float openDuration = 2.5f;

    [Header("Shake Settings")]
    // Reference to the impulse source component on this object
    private CinemachineImpulseSource impulseSource;
    [Tooltip("How strong the shake is")]
    public float shakeForce = 1.0f; 

    private bool isPressed = false;
    private Vector3 initialButtonPos;

    void Start()
    {
        // Auto-get the impulse source component
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (buttonPlunger != null)
            initialButtonPos = buttonPlunger.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPressed) return;
        
        // Optional: Check if the object is actually heavy enough
        // if (other.attachedRigidbody.mass < 5) return;

        ActivateDoor();
    }

    void ActivateDoor()
    {
        isPressed = true;

        // 1. Animate the Button Plunger
        if (buttonPlunger != null)
        {
            buttonPlunger.DOLocalMoveY(initialButtonPos.y + 0.2f, 0.2f)
                .SetEase(Ease.OutQuad);
        }

        // 2. Trigger Cinemachine Shake
        if (impulseSource != null)
        {
            // Generates a shake at this location with the specified force
            impulseSource.GenerateImpulse(shakeForce);
        }

        // 3. Open the Door (Heavy Feel)
        doorObject.DOMoveY(doorObject.position.y + openDistance, openDuration)
            .SetEase(Ease.InCubic); 
        
        // 4. (Optional) Rattle the door mesh while it moves
        doorObject.DOShakePosition(openDuration, new Vector3(0.05f, 0, 0), 5, 90, false, false);
    }
}
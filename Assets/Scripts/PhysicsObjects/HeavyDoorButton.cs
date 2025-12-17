using UnityEngine;
using System.Collections;
using Unity.Cinemachine; 

public class HeavyDoorButton : MonoBehaviour
{
    [Header("Assignments")]
    public Transform doorObject;
    public Transform buttonPlunger; 

    [Header("Door Settings")]
    public float openDistance = 3f;
    public float openDuration = 2.5f;

    [Header("Shake Settings")]
    private CinemachineImpulseSource impulseSource;
    public float shakeForce = 1.0f; 

    private bool isPressed = false;
    private Vector3 initialButtonPos;
    private Vector3 initialDoorPos;

    void Start()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (buttonPlunger != null) initialButtonPos = buttonPlunger.localPosition;
        if (doorObject != null) initialDoorPos = doorObject.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPressed) return;
        Debug.Log("Trigger Hit by: " + other.name);
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        isPressed = true;

        // 1. Plunge the Button (Instant visual feedback)
        if (buttonPlunger != null)
            buttonPlunger.localPosition = new Vector3(initialButtonPos.x, initialButtonPos.y + 0.5f, initialButtonPos.z);

        // 2. Trigger Shake
        if (impulseSource != null)
            impulseSource.GenerateImpulse(shakeForce);

        // 3. Move the Door (The Manual Lerp)
        float elapsedTime = 0;
        Vector3 targetPos = initialDoorPos + new Vector3(0, openDistance, 0);

        // This loop runs every frame for 'openDuration' seconds
        while (elapsedTime < openDuration)
        {
            // "SmoothStep" creates a heavy ease-in/ease-out feel automatically
            float t = elapsedTime / openDuration;
            t = Mathf.SmoothStep(0f, 1f, t); 

            // Move the door
            doorObject.position = Vector3.Lerp(initialDoorPos, targetPos, t);

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for next frame
        }

        // Ensure it ends exactly at the top
        doorObject.position = targetPos;
        Debug.Log("Door Sequence Finished");
    }
}
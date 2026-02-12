using UnityEngine;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float activationMassThreshold = 2.0f;
    [SerializeField] private Vector3 pressedOffset = new Vector3(0, -0.1f, 0);
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private DoorController linkedDoor;
    [SerializeField] private Transform plateVisual;

    private HashSet<Rigidbody2D> objectsOnPlate = new HashSet<Rigidbody2D>();
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isPressed = false;

    private void Start()
    {
        if (plateVisual != null)
        {
            initialPosition = plateVisual.localPosition;
            targetPosition = initialPosition;
        }
    }

    private void Update()
    {
        // Smoothly move to target
        if (plateVisual != null)
        {
            plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, targetPosition, Time.deltaTime * moveSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If it's already pressed forever, stop calculating mass to save performance
        if (isPressed) return;

        if (other.attachedRigidbody != null)
        {
            objectsOnPlate.Add(other.attachedRigidbody);
            CheckMass();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isPressed) return;

        if (other.attachedRigidbody != null)
        {
            objectsOnPlate.Remove(other.attachedRigidbody);
            CheckMass();
        }
    }

    private void CheckMass()
    {
        float currentTotalMass = 0f;

        foreach (Rigidbody2D rb in objectsOnPlate)
        {
            if (rb != null) currentTotalMass += rb.mass;
        }

        if (currentTotalMass >= activationMassThreshold)
        {
            ActivatePlate();
        }
    }

    private void ActivatePlate()
    {
        isPressed = true;
        targetPosition = initialPosition + pressedOffset;
        
        if (linkedDoor != null)
        {
            linkedDoor.Open();
        }

        // Optional: Clear the set since we don't need to track anymore
        objectsOnPlate.Clear();
    }
}
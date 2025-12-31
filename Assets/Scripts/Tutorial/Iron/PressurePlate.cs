using UnityEngine;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The minimum mass required to keep the button pressed.")]
    [SerializeField] private float activationMassThreshold = 2.0f;
    
    [Header("References")]
    [SerializeField] private DoorController linkedDoor;
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.red;

    // We track all objects on the plate to handle multiple lines/objects
    private HashSet<Rigidbody2D> objectsOnPlate = new HashSet<Rigidbody2D>();
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody != null)
        {
            objectsOnPlate.Add(other.attachedRigidbody);
            CheckMass();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody != null)
        {
            objectsOnPlate.Remove(other.attachedRigidbody);
            CheckMass();
        }
    }

    private void CheckMass()
    {
        float currentTotalMass = 0f;

        // Sum up the mass of all objects currently on the plate
        foreach (Rigidbody2D rb in objectsOnPlate)
        {
            // Optional: If you destroy lines, check for null to avoid errors
            if (rb != null) 
            {
                currentTotalMass += rb.mass;
            }
        }

        // Logic Check
        if (currentTotalMass >= activationMassThreshold)
        {
            linkedDoor.Open();
            plateRenderer.color = activeColor;
        }
        else
        {
            linkedDoor.Close();
            plateRenderer.color = inactiveColor;
        }
    }
}
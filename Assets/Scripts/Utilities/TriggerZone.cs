using UnityEngine;

public class TriggerZone2D : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("List of objects to enable/disable")]
    [SerializeField] private GameObject[] objectsToToggle; 

    [Tooltip("The tag of the object that can trigger this zone")]
    [SerializeField] private string targetTag = "Player";

    // Triggered when a 2D collider enters the area
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verify if the object has the correct tag
        if (other.CompareTag(targetTag))
        {
            SetObjectsState(true);
        }
    }

    // Triggered when a 2D collider leaves the area
    private void OnTriggerExit2D(Collider2D other)
    {
        // Verify if the object has the correct tag
        if (other.CompareTag(targetTag))
        {
            SetObjectsState(false);
        }
    }

    // Helper method to update the active state of all assigned objects
    private void SetObjectsState(bool state)
    {
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
        }
    }
}
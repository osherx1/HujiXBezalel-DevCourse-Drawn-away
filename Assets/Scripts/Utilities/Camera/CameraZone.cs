using UnityEngine;
using Unity.Cinemachine; // Use 'Cinemachine' if on older versions

public class CameraZone : MonoBehaviour
{
    [Header("Assign the Specific Camera for this Zone")]
    public CinemachineCamera zoneCamera;

    [Header("Settings")]
    // The priority value when this camera is active (must be higher than default: 10)
    public int activePriority = 20; 
    // The priority when inactive (must be lower or equal to default)
    public int inactivePriority = 10;

    private void Start()
    {
        if (zoneCamera != null)
        {
            // Ensure it starts 'off'
            zoneCamera.Priority = inactivePriority;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            zoneCamera.Priority = activePriority;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            zoneCamera.Priority = inactivePriority;
        }
    }
}
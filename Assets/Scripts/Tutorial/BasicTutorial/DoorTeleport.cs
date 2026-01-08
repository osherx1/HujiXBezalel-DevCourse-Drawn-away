using UnityEngine;

public class DoorTeleport : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform destinationPoint; // Where the player lands
    
    // Optional: Only allow teleport if the door is "unlocked"
    public bool isLocked = true; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object colliding is the Player
        // Make sure your Player object has the tag "Player"
        if (!isLocked && other.CompareTag("Player"))
        {
            Debug.Log("Teleporting player!");
            Debug.Log(other.transform);
            Debug.Log(destinationPoint);
            Debug.Log(SceneTransitionManager.Instance);
            SceneTransitionManager.Instance.TeleportPlayer(other.transform, destinationPoint);
        }
    }
    
    // Helper function to unlock the door (call this from your Tutorial script!)
    public void UnlockDoor()
    {
        isLocked = false;
        // Optional: Change sprite to open door here
    }
}
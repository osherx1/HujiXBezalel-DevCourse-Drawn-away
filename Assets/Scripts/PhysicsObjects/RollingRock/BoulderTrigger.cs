using UnityEngine;

public class BoulderTrigger : MonoBehaviour
{
    [Header("Assignments")]
    public RollingStone stoneController;
    [Tooltip("Assign the Collider of the stone here so we can identify it")]
    public Collider2D stoneCollider;

    // The Lock: Prevents respawning while the stone is inside/passing through
    private bool isTrapLocked = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. IF PLAYER ENTERS: Try to spawn the stone
        if (other.CompareTag("Player"))
        {
            if (!isTrapLocked)
            {
                FireTrap();
            }
        }
        
        // 2. IF STONE ENTERS: Ensure the lock is ON
        // (This catches the case where the stone falls into the trigger)
        if (other == stoneCollider)
        {
            isTrapLocked = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 3. IF STONE EXITS: The coast is clear -> Re-arm the trap!
        if (other == stoneCollider)
        {
            isTrapLocked = false;
            Debug.Log("Stone cleared the zone. Trap re-armed.");
        }
    }

    private void FireTrap()
    {
        Debug.Log("Trap Activated!");
        isTrapLocked = true; // Lock immediately so the player can't double-trigger
        stoneController.ActivateStone();
    }
}
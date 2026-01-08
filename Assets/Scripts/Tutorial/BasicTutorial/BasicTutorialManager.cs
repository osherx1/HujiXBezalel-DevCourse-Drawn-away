using System.Collections;
using UnityEngine;

public class BasicTutorialManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TutorialOverlay uiOverlay;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject roomDoor;
    
    [Header("Step 2: Pickup")]
    [SerializeField] private GameObject materialPickupItem; // The item in the room
    
    [Header("Step 4: Drawing")]
    [SerializeField] private Transform drawingWallTarget; // Where you want them to draw

    private void Start()
    {
        // Close the door initially
        roomDoor.SetActive(true); 
        
        StartCoroutine(MovementState());
    }

    // --- STATE 1: MOVEMENT (WAD) ---
    private IEnumerator MovementState()
    {
        // We don't pause here so they can actually move
        uiOverlay.ShowFocus(null, "Use <b>A</b> and <b>D</b> to move, and <b>W</b> to jump.");

        bool movedLeft = false;
        bool movedRight = false;
        bool jumped = false;

        while (!movedLeft || !movedRight || !jumped)
        {
            // Check for inputs (Replace with your InputSystem calls if needed)
            if (Input.GetKeyDown(KeyCode.A)) movedLeft = true;
            if (Input.GetKeyDown(KeyCode.D)) movedRight = true;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) jumped = true;

            yield return null;
        }

        yield return new WaitForSeconds(0.5f); // Small delay for pacing
        StartCoroutine(PickupState());
    }

    // --- STATE 2: PICK UP MATERIAL ---
    private IEnumerator PickupState()
    {
        // 1. Pause and Highlight the item
        GamePause(true);
        uiOverlay.ShowFocus(materialPickupItem.transform, "There is a material here. Walk over to pick it up.");

        // 2. Wait for player to acknowledge (click to continue) OR just unpause after a moment
        // Let's unpause on click so they can go get it
        yield return WaitForKeyClick(); 

        // 3. Unpause and let them play
        GamePause(false);
        uiOverlay.Hide();

        // 4. Wait until the item is gone (picked up)
        while (materialPickupItem != null && materialPickupItem.activeInHierarchy)
        {
            yield return null;
        }

        StartCoroutine(ToolbarState());
    }

    // --- STATE 3: OPEN TOOLBAR (Right Click) ---
    private IEnumerator ToolbarState()
    {
        yield return new WaitForSeconds(0.5f);

        // Pause and Darken
        GamePause(true);
        // No specific world target, just general instruction
        uiOverlay.ShowFocus(null, "Hold <b>Right Click</b> to open your Material Toolbar.");

        while (!Input.GetMouseButtonDown(1)) // 1 is Right Click
        {
            yield return null;
        }

        // Optional: Wait for them to actually select the item? 
        // For now, we assume opening the menu is enough to see the new item.
        
        uiOverlay.Hide();
        GamePause(false);

        StartCoroutine(DrawingState());
    }

    // --- STATE 4: DRAWING (Left Click) ---
    private IEnumerator DrawingState()
    {
        yield return new WaitForSeconds(0.5f);

        GamePause(true);
        uiOverlay.ShowFocus(drawingWallTarget, "Hold <b>Left Click</b> to draw lines using your selected material.");
        
        yield return WaitForKeyClick(); // Wait for click to resume

        GamePause(false);
        uiOverlay.Hide();

        // Wait for player to actually draw something
        while (!Input.GetMouseButton(0)) // 0 is Left Click
        {
            yield return null;
        }

        // Give them a second to enjoy drawing
        yield return new WaitForSeconds(2.0f);

        FinishTutorial();
    }

    private void FinishTutorial()
    {
        Debug.Log("Tutorial Complete - Opening Door");
        
        // Open the door
        roomDoor.SetActive(false); 
        
        // Optional: Show "Good Job" text
        uiOverlay.ShowFocus(roomDoor.transform, "Great job! Proceed to the next room.");
        Destroy(uiOverlay.gameObject, 3f); // Clean up UI after 3 seconds
    }

    // --- HELPER FUNCTIONS ---

    private void GamePause(bool isPaused)
    {
        // If your game uses physics, TimeScale 0 stops it.
        // If you want them to be able to look around but not move, you'd disable the PlayerController script instead.
        Time.timeScale = isPaused ? 0 : 1;
    }

    private IEnumerator WaitForKeyClick()
    {
        // Waits for a left click to "Close" the dialog and resume gameplay
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
    }
}
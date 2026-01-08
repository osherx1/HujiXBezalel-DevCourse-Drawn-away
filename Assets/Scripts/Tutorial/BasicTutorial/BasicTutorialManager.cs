using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class BasicTutorialManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TutorialOverlay uiOverlay;
    [SerializeField] private GameObject roomDoor;
    
    [Header("Input References (Drag from Project)")]
    // Drag the SAME .inputactions references here that you use in characterInputRelay
    [SerializeField] private InputActionReference moveAction; 
    [SerializeField] private InputActionReference jumpAction;
    
    // You likely have actions for "Fire" (Left Click) and "Secondary" (Right Click)
    // If not, we can fall back to Mouse.current, but Actions are better.
    [SerializeField] private InputActionReference drawAction;    // Left Click
    [SerializeField] private InputActionReference toolbarAction; // Right Click

    [Header("Step 2: Pickup")]
    [SerializeField] private GameObject materialPickupItem; 
    
    [Header("Step 4: Drawing")]
    [SerializeField] private Transform drawingWallTarget;
    
    [Header("Transition")]
    [SerializeField] private SceneTransitionManager transitionManager;
    [SerializeField] private Transform nextRoomSpawnPoint;

    private void Start()
    {
        roomDoor.SetActive(true);
        StartCoroutine(MovementState());
    }

    // --- STATE 1: MOVEMENT (WAD) ---
    private IEnumerator MovementState()
    {
        uiOverlay.ShowFocus(null, "Use <b>A</b> and <b>D</b> to move, and <b>W</b> (or Space) to jump.");

        bool movedLeft = false;
        bool movedRight = false;
        bool jumped = false;

        // 1. Define Callbacks
        System.Action<InputAction.CallbackContext> onMove = (ctx) =>
        {
            float xValue = ctx.ReadValue<Vector2>().x;
            if (xValue < -0.1f) movedLeft = true;
            if (xValue > 0.1f) movedRight = true;
        };

        System.Action<InputAction.CallbackContext> onJump = (ctx) => { jumped = true; };

        // 2. Subscribe to the Action References
        // We use the 'action' property of the Reference
        moveAction.action.performed += onMove;
        jumpAction.action.started += onJump; // Matching your relay script which uses 'started'

        // 3. Wait until task complete
        while (!movedLeft || !movedRight || !jumped)
        {
            yield return null;
        }

        // 4. Unsubscribe (Cleanup)
        moveAction.action.performed -= onMove;
        jumpAction.action.started -= onJump;

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(PickupState());
    }

    // --- STATE 2: PICK UP MATERIAL ---
    private IEnumerator PickupState()
    {
        GamePause(true);
        uiOverlay.ShowFocus(materialPickupItem.transform, "Walk over to the material to pick it up.");

        // Wait for user to acknowledge (Left Click to continue)
        yield return WaitForMouseClick(); 

        GamePause(false);
        uiOverlay.Hide();

        // Wait until the item is destroyed/disabled (picked up)
        while (materialPickupItem != null && materialPickupItem.activeInHierarchy)
        {
            yield return null;
        }

        StartCoroutine(ToolbarState());
    }

    // --- STATE 3: TOOLBAR (Right Click) ---
    private IEnumerator ToolbarState()
    {
        yield return new WaitForSeconds(0.5f);
        GamePause(true);
        uiOverlay.ShowFocus(null, "Hold <b>Right Click</b> to open your Material Toolbar.");

        // Wait specifically for Right Click
        bool rightClicked = false;
        
        // Use Input Action if assigned, otherwise fallback to direct Mouse check
        if (toolbarAction != null)
        {
             System.Action<InputAction.CallbackContext> onRightClick = (ctx) => rightClicked = true;
             toolbarAction.action.performed += onRightClick;
             while(!rightClicked) yield return null;
             toolbarAction.action.performed -= onRightClick;
        }
        else
        {
            // Fallback if you haven't set up a "Toolbar" action yet
            while (!Mouse.current.rightButton.wasPressedThisFrame) yield return null;
        }

        uiOverlay.Hide();
        GamePause(false);
        StartCoroutine(DrawingState());
    }

    // --- STATE 4: DRAWING (Left Click) ---
    private IEnumerator DrawingState()
    {
        yield return new WaitForSeconds(0.5f);
        GamePause(true);
        uiOverlay.ShowFocus(drawingWallTarget, "Hold <b>Left Click</b> to draw lines.");
        
        yield return WaitForMouseClick(); // Acknowledge text

        GamePause(false);
        uiOverlay.Hide();

        // Wait for Drawing (Left Click)
        bool drewLine = false;

        if (drawAction != null)
        {
             System.Action<InputAction.CallbackContext> onDraw = (ctx) => drewLine = true;
             drawAction.action.performed += onDraw;
             while(!drewLine) yield return null;
             drawAction.action.performed -= onDraw;
        }
        else
        {
             // Fallback
             while (!Mouse.current.leftButton.isPressed) yield return null;
        }

        yield return new WaitForSeconds(2.0f); // Let them draw a bit
        FinishTutorial();
    }

    private void FinishTutorial()
    {
        Debug.Log("Tutorial Complete");
        roomDoor.SetActive(false); // Open Door visually (optional)
        
        // Trigger the Fade and Teleport
        if(transitionManager != null)
        {
            transitionManager.TeleportPlayer(GameObject.FindGameObjectWithTag("Player").transform, nextRoomSpawnPoint);
        }
        
        Destroy(uiOverlay.gameObject); 
    }

    // --- HELPERS ---
    
    private void GamePause(bool isPaused)
    {
        Time.timeScale = isPaused ? 0 : 1;
    }

    private IEnumerator WaitForMouseClick()
    {
        // Simple loop waiting for left click to advance text
        while (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            yield return null;
        }
        // Small delay to prevent clicking through everything instantly
        yield return new WaitForSecondsRealtime(0.2f);
    }
}
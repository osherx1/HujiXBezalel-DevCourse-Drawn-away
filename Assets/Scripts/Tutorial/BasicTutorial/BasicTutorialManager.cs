using System.Collections;
using Drawing.Buttons;
using Drawing.LineControl;
using Drawing.Managers;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;

public class BasicTutorialManager : MonoBehaviour
{
    [Header("--- TUTORIAL UI ---")]
    [SerializeField] private TutorialOverlay uiOverlay;
    
    [Header("--- INSTRUCTION SPRITES ---")]
    [Tooltip("W/A/D/Space")]
    [SerializeField] private Sprite spriteMovement;
    [Tooltip("Walk to material")]
    [SerializeField] private Sprite spritePickup;
    [Tooltip("Right Click to Open")]
    [SerializeField] private Sprite spriteOpenToolbar;
    [Tooltip("Select Material Icon")]
    [SerializeField] private Sprite spriteSelectMaterial;
    [Tooltip("Hold Left Click to Draw")]
    [SerializeField] private Sprite spriteDraw;
    [Tooltip("Select Broom Icon")]
    [SerializeField] private Sprite spriteSelectBroom;
    [Tooltip("Drag over lines")]
    [SerializeField] private Sprite spriteEraseAction;
    [Tooltip("Draw more lines (if board is empty)")]
    [SerializeField] private Sprite spriteDrawAgain; 
    [Tooltip("Select Reset Icon")]
    [SerializeField] private Sprite spriteSelectReset;
    [Tooltip("Close the menu")]
    [SerializeField] private Sprite spriteCloseMenu;

    [Header("--- GAME OBJECT REFERENCES ---")]
    [SerializeField] private GameObject toolbarPanelObject; 
    [SerializeField] private LineManager lineManager;
    [SerializeField] private Transform linesRoot; 
    [SerializeField] private GameObject materialPickupItem; 
    [SerializeField] private SpriteRenderer roomDoor;
    [SerializeField] private DoorTeleport doorTeleport;

    [Header("--- BUTTONS (For Locking) ---")]
    [SerializeField] private Button materialButton;
    [SerializeField] private Button broomButton;
    [SerializeField] private Button resetButton;

    [Header("--- LOCATIONS ---")]
    [SerializeField] private Transform drawingWallTarget;   
    [SerializeField] private Transform eraseTarget;         

    [Header("--- INPUT REFERENCES ---")]
    [SerializeField] private InputActionReference moveAction; 
    [SerializeField] private InputActionReference jumpAction;

    // State Flags
    private bool _materialSelected = false;
    private bool _eraserActive = false;
    private bool _boardReset = false;
    private bool _lineDrawn = false;
    private bool _lineDestroyed = false;

    private void Start()
    {
        // Start with all buttons locked to prevent early clicking
        SetButtonsActive(false, false, false);
        StartCoroutine(TutorialSequence());
    }

    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnConfigButtonSelected += HandleMaterialSelected;
            EventManager.Instance.OnEraserActive += HandleEraserActive;
            EventManager.Instance.OnBoardReset += HandleBoardReset;
        }

        if (lineManager != null)
        {
            lineManager.OnLineFinished += HandleLineDrawn;
        }

        Line.onLineDestroyed += HandleLineDestroyed;
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnConfigButtonSelected -= HandleMaterialSelected;
            EventManager.Instance.OnEraserActive -= HandleEraserActive;
            EventManager.Instance.OnBoardReset -= HandleBoardReset;
        }

        if (lineManager != null)
        {
            lineManager.OnLineFinished -= HandleLineDrawn;
        }

        Line.onLineDestroyed -= HandleLineDestroyed;
    }

    // --- EVENT LISTENERS ---
    private void HandleMaterialSelected(object sender) => _materialSelected = true;
    private void HandleEraserActive() => _eraserActive = true;
    private void HandleBoardReset() => _boardReset = true;
    private void HandleLineDrawn(Line line) => _lineDrawn = true;
    private void HandleLineDestroyed(string id, int cost) => _lineDestroyed = true;

    // --- MAIN SEQUENCE ---
    private IEnumerator TutorialSequence()
    {
        // 1. MOVEMENT
        yield return MovementState();

        // 2. PICKUP
        yield return PickupState();

        // 3. TOOLBAR & MATERIALS
        yield return MaterialTutorialState();

        // 4. DRAWING
        yield return DrawingState();

        // 5. BROOM (ERASER)
        yield return BroomState();

        // 6. PREPARE FOR RESET (Ensure lines exist)
        yield return PrepareResetState();

        // 7. RESET
        yield return ResetState();

        // 8. FINISH
        FinishTutorial();
    }

    // --- STEPS ---

    private IEnumerator MovementState()
    {
        // Show instruction immediately. No pause.
        uiOverlay.ShowFocus(null, spriteMovement);

        bool movedLeft = false;
        bool movedRight = false;
        bool jumped = false;

        System.Action<InputAction.CallbackContext> onMove = (ctx) =>
        {
            float x = ctx.ReadValue<Vector2>().x;
            if (x < -0.1f) movedLeft = true;
            if (x > 0.1f) movedRight = true;
        };
        System.Action<InputAction.CallbackContext> onJump = (ctx) => jumped = true;

        if(moveAction != null) moveAction.action.performed += onMove;
        if(jumpAction != null) jumpAction.action.started += onJump;

        // Wait strictly for actions
        while (!movedLeft || !movedRight || !jumped)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame) movedLeft = true;
                if (Keyboard.current.dKey.wasPressedThisFrame) movedRight = true;
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) jumped = true;
            }
            yield return null;
        }

        if(moveAction != null) moveAction.action.performed -= onMove;
        if(jumpAction != null) jumpAction.action.started -= onJump;

        // Transition: Hide overlay briefly or keep it for next step?
        // Usually good to hide briefly to show success, or just swap instantly.
        uiOverlay.Hide(); 
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator PickupState()
    {
        uiOverlay.ShowFocus(materialPickupItem.transform, spritePickup);

        // Wait until item is gone
        while (materialPickupItem != null && materialPickupItem.activeInHierarchy)
        {
            yield return null;
        }
        
        uiOverlay.Hide();
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator MaterialTutorialState()
    {
        // A. Open Toolbar
        uiOverlay.ShowFocus(null, spriteOpenToolbar);

        // Wait for toolbar to open
        while (!IsToolbarOpen())
        {
            yield return null;
        }
        
        // LOCK: Only Material
        SetButtonsActive(true, false, false);

        // B. Select Material
        uiOverlay.ShowFocus(null, spriteSelectMaterial);

        _materialSelected = false;
        while (!_materialSelected)
        {
            yield return null;
        }
        
        uiOverlay.Hide();

        // Wait for auto-close (handled by your UI logic)
        while (IsToolbarOpen()) yield return null;
    }

    private IEnumerator DrawingState()
    {
        uiOverlay.ShowFocus(drawingWallTarget, spriteDraw);

        _lineDrawn = false;
        while (!_lineDrawn)
        {
            yield return null;
        }
        
        uiOverlay.Hide();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator BroomState()
    {
        // A. Open Toolbar
        uiOverlay.ShowFocus(null, spriteOpenToolbar);
        while (!IsToolbarOpen()) yield return null;
        
        // LOCK: Only Broom
        SetButtonsActive(false, true, false);

        // B. Select Broom
        uiOverlay.ShowFocus(null, spriteSelectBroom);
        
        _eraserActive = false;
        while (!_eraserActive) yield return null;
        
        uiOverlay.Hide();
        while (IsToolbarOpen()) yield return null;

        // C. Erase Action
        uiOverlay.ShowFocus(eraseTarget, spriteEraseAction);
        
        _lineDestroyed = false;
        while (!_lineDestroyed)
        {
            yield return null;
        }
        
        uiOverlay.Hide();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator PrepareResetState()
    {
        // Ensure there is something to reset
        if (GetLineCount() == 0)
        {
            // Unlock Material button again so they can draw
            SetButtonsActive(true, false, false);
            
            // Show "Draw Again" instruction
            uiOverlay.ShowFocus(drawingWallTarget, spriteDrawAgain);
            
            // Wait until lines exist. 
            // Note: If the toolbar is closed, the player needs to open it. 
            // We trust the player remembers how to open it, or the sprite implies it.
            while (GetLineCount() == 0)
            {
                yield return null;
            }
            
            uiOverlay.Hide();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ResetState()
    {
        // A. Open Toolbar
        uiOverlay.ShowFocus(null, spriteOpenToolbar);
        while (!IsToolbarOpen()) yield return null;

        // LOCK: Only Reset
        SetButtonsActive(false, false, true);

        // B. Click Reset
        uiOverlay.ShowFocus(null, spriteSelectReset);
        
        _boardReset = false;
        while (!_boardReset)
        {
            yield return null;
        }
        
        uiOverlay.Hide();
        yield return new WaitForSeconds(0.5f);
        
        // C. Close Menu
        if (IsToolbarOpen())
        {
            uiOverlay.ShowFocus(null, spriteCloseMenu);
            // Unlock tools so they can click one to close, or right click
            SetButtonsActive(true, true, true);
            
            while (IsToolbarOpen()) yield return null;
            uiOverlay.Hide();
        }
    }

    private void FinishTutorial()
    {
        // Final Cleanup
        SetButtonsActive(true, true, true); // Enable all for gameplay
        
        if(roomDoor != null) roomDoor.sprite = null; 
        if(doorTeleport != null) doorTeleport.UnlockDoor();
        
        if(uiOverlay != null) 
        {
            uiOverlay.Hide(); 
            Destroy(uiOverlay.gameObject);
        }
    }

    // --- HELPERS ---

    private void SetButtonsActive(bool material, bool broom, bool reset)
    {
        if (materialButton) materialButton.interactable = material;
        if (broomButton) broomButton.interactable = broom;
        if (resetButton) resetButton.interactable = reset;
    }

    private int GetLineCount()
    {
        if (linesRoot != null) return linesRoot.childCount;
        return 0;
    }

    private bool IsToolbarOpen()
    {
        return toolbarPanelObject != null && toolbarPanelObject.activeInHierarchy;
    }
}
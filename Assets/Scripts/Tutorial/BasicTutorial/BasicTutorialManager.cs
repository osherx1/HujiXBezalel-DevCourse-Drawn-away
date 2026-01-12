using System.Collections;
using Drawing.Buttons;
using Drawing.LineControl;
using Drawing.Managers;
using Game.Core.Tutorial; // Namespace for TutorialTextRenderer
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BasicTutorialManager : MonoBehaviour
{
    [Header("--- RENDERER INTEGRATION ---")]
    [SerializeField] private TutorialTextRenderer tutorialRenderer;
    [Tooltip("The full-screen dark panel (Image) to fade in/out.")]
    [SerializeField] private Image blurPanel;
    [Tooltip("The UI Image component that will display the Instruction Sprites.")]
    [SerializeField] private Image instructionImage;
    [Tooltip("The Object with the Mask component (Spotlight). Moves to highlight targets.")]
    [SerializeField] private RectTransform spotlightTransform;
    [SerializeField] private Camera mainCamera;

    [Header("--- INSTRUCTION SPRITES ---")]
    [SerializeField] private Sprite spriteMovement;
    [SerializeField] private Sprite spritePickup;
    [SerializeField] private Sprite spriteOpenToolbar;
    [SerializeField] private Sprite spriteSelectMaterial;
    [SerializeField] private Sprite spriteDraw;
    [SerializeField] private Sprite spriteSelectBroom;
    [SerializeField] private Sprite spriteEraseAction;
    [SerializeField] private Sprite spriteDrawAgain;
    [SerializeField] private Sprite spriteSelectReset;
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
        // 1. Setup Direct Listeners
        if (materialButton != null) materialButton.onClick.AddListener(() => _materialSelected = true);
        if (broomButton != null) broomButton.onClick.AddListener(() => _eraserActive = true);
        if (resetButton != null) resetButton.onClick.AddListener(() => _boardReset = true);

        // 2. Lock all buttons initially
        SetButtonsActive(false, false, false);
        
        // 3. Ensure Camera reference
        if (mainCamera == null) mainCamera = Camera.main;

        StartCoroutine(TutorialSequence());
    }

    private void OnEnable()
    {
        if (lineManager != null) lineManager.OnLineFinished += HandleLineDrawn;
        Line.onLineDestroyed += HandleLineDestroyed;
    }

    private void OnDisable()
    {
        if (lineManager != null) lineManager.OnLineFinished -= HandleLineDrawn;
        Line.onLineDestroyed -= HandleLineDestroyed;
    }

    // private void OnDestroy()
    // {
    //     if (materialButton != null) materialButton.onClick.RemoveAllListeners();
    //     if (broomButton != null) broomButton.onClick.RemoveAllListeners();
    //     if (resetButton != null) resetButton.onClick.RemoveAllListeners();
    // }

    // --- EVENT LISTENERS ---
    private void HandleLineDrawn(Line line) => _lineDrawn = true;
    private void HandleLineDestroyed(string id, int cost) => _lineDestroyed = true;

    // --- MAIN SEQUENCE ---
    private IEnumerator TutorialSequence()
    {
        // 1. MOVEMENT & PICKUP
        yield return MovementAndPickupPhase();

        // 3. TOOLBAR & MATERIALS
        yield return MaterialTutorialState();

        // 4. DRAWING
        yield return DrawingState();

        // 5. BROOM (ERASER)
        yield return BroomState();

        // 6. PREPARE FOR RESET
        yield return PrepareResetState();

        // 7. RESET
        yield return ResetState();

        // 8. FINISH
        FinishTutorial();
    }

    // --- STEPS ---

    private IEnumerator MovementAndPickupPhase()
    {
        // Start Instruction: Movement
        ShowInstruction(spriteMovement, null);

        bool movedLeft = false;
        bool movedRight = false;
        bool jumped = false;

        // Input monitoring
        System.Action<InputAction.CallbackContext> onMove = (ctx) =>
        {
            float x = ctx.ReadValue<Vector2>().x;
            if (x < -0.1f) movedLeft = true;
            if (x > 0.1f) movedRight = true;
        };
        System.Action<InputAction.CallbackContext> onJump = (ctx) => jumped = true;

        if (moveAction != null) moveAction.action.performed += onMove;
        if (jumpAction != null) jumpAction.action.started += onJump;

        // Wait for movement OR item pickup (skip if player rushes)
        while (!IsItemPickedUp())
        {
            if (movedLeft && movedRight && jumped) break;

            // Keyboard Fallback
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame) movedLeft = true;
                if (Keyboard.current.dKey.wasPressedThisFrame) movedRight = true;
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) jumped = true;
            }
            yield return null;
        }

        if (moveAction != null) moveAction.action.performed -= onMove;
        if (jumpAction != null) jumpAction.action.started -= onJump;

        // --- PICKUP PHASE ---
        if (!IsItemPickedUp())
        {
            // Transition visual to Pickup
            // We hide briefly or just swap. Let's swap via ShowInstruction directly for smoothness
            ShowInstruction(spritePickup, materialPickupItem.transform);

            while (!IsItemPickedUp())
            {
                // Update spotlight position dynamically in case player pushes item
                UpdateSpotlightPosition(materialPickupItem.transform);
                yield return null;
            }
        }

        HideInstruction();
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator MaterialTutorialState()
    {
        // A. Open Toolbar
        ShowInstruction(spriteOpenToolbar, null);

        while (!IsToolbarOpen()) yield return null;

        // UNLOCK: Material Button
        SetButtonsActive(true, false, false);

        // B. Select Material
        // Note: No specific target for the button unless we have its RectTransform, so null target
        ShowInstruction(spriteSelectMaterial, null);

        _materialSelected = false;
        while (!_materialSelected)
        {
            if (!IsToolbarOpen())
            {
                // Re-prompt if they closed it
                ShowInstruction(spriteOpenToolbar, null);
                while (!IsToolbarOpen()) yield return null;
                ShowInstruction(spriteSelectMaterial, null);
            }
            yield return null;
        }

        HideInstruction();
        // Wait for auto-close
        while (IsToolbarOpen()) yield return null;
    }

    private IEnumerator DrawingState()
    {
        ShowInstruction(spriteDraw, drawingWallTarget);

        _lineDrawn = false;
        while (!_lineDrawn) yield return null;

        HideInstruction();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator BroomState()
    {
        // A. Open Toolbar
        ShowInstruction(spriteOpenToolbar, null);
        while (!IsToolbarOpen()) yield return null;

        // LOCK: Only Broom
        SetButtonsActive(false, true, false);

        // B. Select Broom
        ShowInstruction(spriteSelectBroom, null);

        _eraserActive = false;
        while (!_eraserActive)
        {
            if (!IsToolbarOpen())
            {
                ShowInstruction(spriteOpenToolbar, null);
                while (!IsToolbarOpen()) yield return null;
                ShowInstruction(spriteSelectBroom, null);
            }
            yield return null;
        }

        HideInstruction();
        while (IsToolbarOpen()) yield return null;

        // C. Erase Action
        ShowInstruction(spriteEraseAction, eraseTarget);

        _lineDestroyed = false;
        while (!_lineDestroyed) yield return null;

        HideInstruction();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator PrepareResetState()
    {
        // Check if we need lines
        if (GetLineCount() == 0)
        {
            // Unlock Material button again
            SetButtonsActive(true, false, false);

            ShowInstruction(spriteDrawAgain, drawingWallTarget);

            while (GetLineCount() == 0) yield return null;

            HideInstruction();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ResetState()
    {
        // A. Open Toolbar
        ShowInstruction(spriteOpenToolbar, null);
        while (!IsToolbarOpen()) yield return null;

        // LOCK: Only Reset
        SetButtonsActive(false, false, true);

        // B. Click Reset
        ShowInstruction(spriteSelectReset, null);

        _boardReset = false;
        while (!_boardReset)
        {
            if (!IsToolbarOpen())
            {
                ShowInstruction(spriteOpenToolbar, null);
                while (!IsToolbarOpen()) yield return null;
                ShowInstruction(spriteSelectReset, null);
            }
            yield return null;
        }

        HideInstruction();
        yield return new WaitForSeconds(0.5f);

        // C. Close Menu
        if (IsToolbarOpen())
        {
            ShowInstruction(spriteCloseMenu, null);
            SetButtonsActive(true, true, true); // Unlock all

            while (IsToolbarOpen()) yield return null;
            HideInstruction();
        }
    }

    private void FinishTutorial()
    {
        SetButtonsActive(true, true, true);

        if (roomDoor != null) roomDoor.sprite = null;
        if (doorTeleport != null) doorTeleport.UnlockDoor();

        // Optional: Hide Renderer one last time just in case
        if (tutorialRenderer != null)
        {
            tutorialRenderer.HideBlurAndImages(blurPanel, new Image[] { instructionImage });
        }
    }

    // --- HELPER FUNCTIONS ---

    private void ShowInstruction(Sprite sprite, Transform target)
    {
        // 1. Setup Sprite
        if (instructionImage != null)
        {
            instructionImage.sprite = sprite;
            instructionImage.preserveAspect = true;
        }

        // 2. Setup Spotlight (Visual Position)
        if (spotlightTransform != null)
        {
            if (target != null)
            {
                spotlightTransform.gameObject.SetActive(true);
                UpdateSpotlightPosition(target);
            }
            else
            {
                // If no target, we hide the spotlight (solid blur)
                spotlightTransform.gameObject.SetActive(false);
            }
        }

        // 3. Fade In via Renderer
        if (tutorialRenderer != null && blurPanel != null)
        {
            tutorialRenderer.ShowBlurAndImages(blurPanel, new Image[] { instructionImage });
        }
    }

    private void HideInstruction()
    {
        if (tutorialRenderer != null && blurPanel != null)
        {
            tutorialRenderer.HideBlurAndImages(blurPanel, new Image[] { instructionImage });
        }
    }

    private void UpdateSpotlightPosition(Transform target)
    {
        if (spotlightTransform != null && target != null && mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
            spotlightTransform.position = screenPos;
        }
    }

    private bool IsItemPickedUp()
    {
        return materialPickupItem == null || !materialPickupItem.activeInHierarchy;
    }

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
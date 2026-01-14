using System.Collections;
using Drawing.Buttons;
using Drawing.LineControl;
using Drawing.Managers;
using Game.Core.Tutorial; 
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BasicTutorialManager : MonoBehaviour
{
    [Header("--- RENDERER INTEGRATION ---")]
    [SerializeField] private TutorialTextRenderer tutorialRenderer;
    [Tooltip("The full-screen dark panel.")]
    [SerializeField] private Image blurPanel;
    [Tooltip("The UI Image for the Sprite.")]
    [SerializeField] private Image instructionImage;
    [Tooltip("The Grey Rectangle behind the sprite.")]
    [SerializeField] private Image instructionBackground; 
    [Tooltip("The Object with the Mask component (Spotlight).")]
    [SerializeField] private RectTransform spotlightTransform;
    [SerializeField] private Camera mainCamera;

    [Header("--- INSTRUCTION SPRITES ---")]
    [SerializeField] private Sprite spriteMovement;
    [SerializeField] private Sprite spritePickup;
    [SerializeField] private Sprite spriteOpenToolbar;
    [SerializeField] private Sprite spriteSelectMaterial;
    [SerializeField] private Sprite spriteMaterialUiInfo; // New: Info about top-left UI
    [SerializeField] private Sprite spriteDraw;
    [SerializeField] private Sprite spriteSelectBroom;
    [SerializeField] private Sprite spriteEraseAction;
    [SerializeField] private Sprite spriteSelectMaterialAgain; // New: Select material 2nd time
    [SerializeField] private Sprite spriteDrawAgain;      
    [SerializeField] private Sprite spriteSelectReset;
    [SerializeField] private Sprite spriteGoToDoor;       

    [Header("--- GAME OBJECT REFERENCES ---")]
    [SerializeField] private GameObject toolbarPanelObject;
    [SerializeField] private LineManager lineManager;
    [SerializeField] private Transform linesRoot;
    [SerializeField] private GameObject materialPickupItem;
    [SerializeField] private SpriteRenderer roomDoor;
    [SerializeField] private DoorTeleport doorTeleport;

    [Header("--- BUTTONS & UI TARGETS ---")]
    [SerializeField] private Button materialButton;
    [SerializeField] private Button broomButton;
    [SerializeField] private Button resetButton;
    [Tooltip("The UI element in the top-left corner to highlight.")]
    [SerializeField] private RectTransform materialInfoUiTarget;

    [Header("--- SETTINGS ---")]
    [SerializeField] private float doorSlideHeight = 4f;
    [SerializeField] private float doorSlideDuration = 2f;

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
        // Setup Listeners
        if (materialButton != null) materialButton.onClick.AddListener(() => _materialSelected = true);
        if (broomButton != null) broomButton.onClick.AddListener(() => _eraserActive = true);
        if (resetButton != null) resetButton.onClick.AddListener(() => _boardReset = true);

        // Initial Lock
        SetButtonsActive(false, false, false);
        
        if (mainCamera == null) mainCamera = Camera.main;
        if (instructionBackground != null) instructionBackground.gameObject.SetActive(false);

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

    private void OnDestroy()
    {
        if (materialButton != null) materialButton.onClick.RemoveAllListeners();
        if (broomButton != null) broomButton.onClick.RemoveAllListeners();
        if (resetButton != null) resetButton.onClick.RemoveAllListeners();
    }

    private void HandleLineDrawn(Line line) => _lineDrawn = true;
    private void HandleLineDestroyed(string id, int cost) => _lineDestroyed = true;

    // --- MAIN SEQUENCE ---
    private IEnumerator TutorialSequence()
    {
        yield return MovementPhase();
        yield return PickupPhase();
        yield return SelectMaterialPhase(isFirstTime: true);
        yield return DrawingPhase();
        yield return MaterialUiInfoPhase();
        yield return BroomPhase();
        yield return SelectMaterialPhase(isFirstTime: false); // Select material again
        yield return DrawAgainPhase();
        yield return ResetPhase();
        yield return FinishPhase();
    }

    // --- STEPS ---

    private IEnumerator MovementPhase()
    {
        ShowInstruction(spriteMovement, null);

        bool movedLeft = false, movedRight = false, jumped = false;
        System.Action<InputAction.CallbackContext> onMove = (ctx) => { if (ctx.ReadValue<Vector2>().x < -0.1f) movedLeft = true; if (ctx.ReadValue<Vector2>().x > 0.1f) movedRight = true; };
        System.Action<InputAction.CallbackContext> onJump = (ctx) => jumped = true;

        if (moveAction != null) moveAction.action.performed += onMove;
        if (jumpAction != null) jumpAction.action.started += onJump;

        while (!IsItemPickedUp())
        {
            if (movedLeft && movedRight && jumped) break;
            // Keyboard Fallback
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame) movedLeft = true;
                if (Keyboard.current.dKey.wasPressedThisFrame) movedRight = true;
                if (Keyboard.current.spaceKey.wasPressedThisFrame) jumped = true;
            }
            yield return null;
        }

        if (moveAction != null) moveAction.action.performed -= onMove;
        if (jumpAction != null) jumpAction.action.started -= onJump;

        HideInstruction();
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator PickupPhase()
    {
        if (!IsItemPickedUp())
        {
            ShowInstruction(spritePickup, materialPickupItem.transform);
            while (!IsItemPickedUp())
            {
                UpdateSpotlightPosition(materialPickupItem.transform);
                yield return null;
            }
        }
        HideInstruction();
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator SelectMaterialPhase(bool isFirstTime)
    {
        // 1. Open Toolbar (if not already open)
        if (!IsToolbarOpen())
        {
            ShowInstruction(spriteOpenToolbar, null);
            while (!IsToolbarOpen()) yield return null;
        }

        // 2. Spotlight the Material Button
        Transform target = materialButton != null ? materialButton.transform : null;
        Sprite spriteToUse = isFirstTime ? spriteSelectMaterial : spriteSelectMaterialAgain;
        
        ShowInstruction(spriteToUse, target);

        SetButtonsActive(true, false, false); // Unlock Material, Lock others
        _materialSelected = false;
        
        while (!_materialSelected)
        {
            // Safety: if they closed it, ask to open again
            if (!IsToolbarOpen())
            {
                ShowInstruction(spriteOpenToolbar, null);
                while (!IsToolbarOpen()) yield return null;
                ShowInstruction(spriteToUse, target);
            }
            yield return null;
        }

        HideInstruction();
        
        // Wait for toolbar to close (Assuming picking a material auto-closes it)
        while (IsToolbarOpen()) yield return null;
        yield return new WaitForSeconds(0.2f); 
    }

    private IEnumerator DrawingPhase()
    {
        ShowInstruction(spriteDraw, null);
        
        _lineDrawn = false;
        while (!_lineDrawn) yield return null;

        HideInstruction();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MaterialUiInfoPhase()
    {
        // Spotlight the Top-Left UI
        // We use the 'materialInfoUiTarget' RectTransform
        ShowInstruction(spriteMaterialUiInfo, materialInfoUiTarget);

        // Wait for user click to acknowledge
        yield return WaitForMouseClick();

        HideInstruction();
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator BroomPhase()
    {
        // 1. Open Toolkit
        if (!IsToolbarOpen())
        {
            ShowInstruction(spriteOpenToolbar, null);
            while (!IsToolbarOpen()) yield return null;
        }

        // 2. Select Broom
        Transform target = broomButton != null ? broomButton.transform : null;
        ShowInstruction(spriteSelectBroom, target);
        
        SetButtonsActive(false, true, false); // Lock all but Broom
        _eraserActive = false;

        while (!_eraserActive)
        {
            if (!IsToolbarOpen())
            {
                ShowInstruction(spriteOpenToolbar, null);
                while (!IsToolbarOpen()) yield return null;
                ShowInstruction(spriteSelectBroom, target);
            }
            yield return null;
        }
        HideInstruction();
        while (IsToolbarOpen()) yield return null;

        // 3. Erase Lines
        ShowInstruction(spriteEraseAction, null);
        _lineDestroyed = false;
        while (!_lineDestroyed) yield return null;

        HideInstruction();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator DrawAgainPhase()
    {
        // Info: We already re-selected the material in the previous step "SelectMaterialPhase(false)"
        // So we just ask to draw now.

        ShowInstruction(spriteDrawAgain, null);
        
        _lineDrawn = false; 
        while (!_lineDrawn) yield return null;

        HideInstruction();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ResetPhase()
    {
        // 1. Open Toolkit
        if (!IsToolbarOpen())
        {
            ShowInstruction(spriteOpenToolbar, null);
            while (!IsToolbarOpen()) yield return null;
        }

        // 2. Choose Reset
        Transform target = resetButton != null ? resetButton.transform : null;
        ShowInstruction(spriteSelectReset, target);
        
        SetButtonsActive(false, false, true); // Unlock Reset
        _boardReset = false;

        while (!_boardReset)
        {
             if (!IsToolbarOpen()) 
             {
                ShowInstruction(spriteOpenToolbar, null);
                while (!IsToolbarOpen()) yield return null;
                ShowInstruction(spriteSelectReset, target);
             }
             yield return null;
        }

        HideInstruction();
        
        // Wait for Reset logic to potentially keep menu open or closed
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator FinishPhase()
    {
        // Unlock everything
        SetButtonsActive(true, true, true); 
        if (doorTeleport != null) doorTeleport.UnlockDoor();

        // SLIDE DOOR UP
        if (roomDoor != null)
        {
            Vector3 startPos = roomDoor.transform.position;
            Vector3 endPos = startPos + Vector3.up * doorSlideHeight;
            float elapsed = 0f;

            while (elapsed < doorSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / doorSlideDuration;
                // Smooth ease out
                t = Mathf.Sin(t * Mathf.PI * 0.5f); 
                roomDoor.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            roomDoor.transform.position = endPos;
        }

        // Show "Go to Door" instruction
        ShowInstruction(spriteGoToDoor, roomDoor.transform);

        // Hide instruction after a few seconds or when player leaves
        yield return new WaitForSeconds(4f);
        HideInstruction();
    }

    // --- HELPER FUNCTIONS ---

    private void ShowInstruction(Sprite sprite, Transform target)
    {
        if (instructionImage != null)
        {
            instructionImage.sprite = sprite;
            instructionImage.preserveAspect = true;
        }
        
        // Enable Grey Background
        if (instructionBackground != null)
        {
            instructionBackground.gameObject.SetActive(true);
        }

        if (spotlightTransform != null)
        {
            if (target != null)
            {
                spotlightTransform.gameObject.SetActive(true);
                UpdateSpotlightPosition(target);
            }
            else
            {
                spotlightTransform.gameObject.SetActive(false);
            }
        }

        if (tutorialRenderer != null && blurPanel != null)
        {
            // Fade in both the background and the sprite
            Image[] imagesToShow = instructionBackground != null 
                ? new Image[] { instructionBackground, instructionImage } 
                : new Image[] { instructionImage };

            tutorialRenderer.ShowBlurAndImages(blurPanel, imagesToShow);
        }
    }

    private void HideInstruction()
    {
        if (tutorialRenderer != null && blurPanel != null)
        {
            Image[] imagesToHide = instructionBackground != null 
                ? new Image[] { instructionBackground, instructionImage } 
                : new Image[] { instructionImage };

            tutorialRenderer.HideBlurAndImages(blurPanel, imagesToHide);
        }
    }

    private void UpdateSpotlightPosition(Transform target)
    {
        if (spotlightTransform == null || target == null || mainCamera == null) return;

        Vector3 screenPos;
        Canvas rootCanvas = target.GetComponentInParent<Canvas>();
        
        // If target is UI (Overlay), use its position directly. If World, convert.
        if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            screenPos = target.position;
        }
        else
        {
            screenPos = mainCamera.WorldToScreenPoint(target.position);
        }

        spotlightTransform.position = screenPos;
    }

    private IEnumerator WaitForMouseClick()
    {
        while (Mouse.current != null && !Mouse.current.leftButton.wasPressedThisFrame)
        {
            yield return null;
        }
    }

    private bool IsItemPickedUp() => materialPickupItem == null || !materialPickupItem.activeInHierarchy;
    
    private void SetButtonsActive(bool material, bool broom, bool reset)
    {
        if (materialButton) materialButton.interactable = material;
        if (broomButton) broomButton.interactable = broom;
        if (resetButton) resetButton.interactable = reset;
    }

    private bool IsToolbarOpen() => toolbarPanelObject != null && toolbarPanelObject.activeInHierarchy;
}
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Drawing.Buttons; // For MenuController

public class NewTutorialManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private TutorialOverlayController overlay;
    [SerializeField] private MenuController menuController;
    [SerializeField] private DoorTeleport doorTeleport;
    [SerializeField] private SpriteRenderer roomDoor;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteMovementControls;
    [SerializeField] private Sprite spriteMaterialInfo;
    [SerializeField] private Sprite spriteBroomInfo;
    [SerializeField] private Sprite spriteResetInfo;
    [SerializeField] private Sprite spriteDrawInstruction; // For Level 2 logic

    [Header("References")]
    [SerializeField] private GameObject materialPickupItem; // To detect pickup
    [SerializeField] private RectTransform materialBtn;
    [SerializeField] private RectTransform broomBtn;
    [SerializeField] private RectTransform resetBtn;

    [Header("Settings")]
    [SerializeField] private float movementPhaseDuration = 5.0f;
    [SerializeField] private float doorSlideHeight = 4f;

    [Header("Inputs")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference submitAction; // "Enter" key
    
    [Header("Level Settings")]
    [Tooltip("Check this if this script is in Level 2 (Skip movement/veil, just show Draw).")]
    [SerializeField] private bool isLevel2 = false;

    // Internal State
    private bool _pickupCollected = false;

    private void Start()
    {
        if (overlay) overlay.Initialize();
        
        if (submitAction != null) submitAction.action.Enable();

        if (isLevel2)
        {
            // DO NOTHING. 
            // We are in Level 2, so we wait for the TutorialTrigger to call StartLevel2Instruction().
        }
        else
        {
            // LEVEL 1: Auto-start the full flow immediately
            StartCoroutine(TutorialFlow());
        }
    }

    private IEnumerator TutorialFlow()
    {
        // 1. MOVEMENT PHASE
        yield return MovementPhase();

        // 2. WAIT FOR PICKUP
        // We wait passively until the item is gone from the scene
        while (materialPickupItem != null && materialPickupItem.activeInHierarchy)
        {
            yield return null;
        }

        // 3. TOOLBAR SEQUENCE
        yield return ToolbarSequence();

        // 4. FINISH
        yield return FinishSequence();
    }

    // --- PHASES ---

    private IEnumerator MovementPhase()
    {
        overlay.ShowSimpleSprite(spriteMovementControls);

        float timer = 0f;
        bool w = false, a = false, d = false;

        // Wait until time up OR all keys pressed
        while (timer < movementPhaseDuration)
        {
            timer += Time.deltaTime;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame) w = true;
                if (Keyboard.current.aKey.wasPressedThisFrame) a = true;
                if (Keyboard.current.dKey.wasPressedThisFrame) d = true;
            }

            // Early exit if user proved they can move
            if (w && a && d) break;

            yield return null;
        }

        overlay.HideSimpleSprite();
    }

    private IEnumerator ToolbarSequence()
    {
        // Force Open Toolbar
        if (menuController) menuController.OpenMenu();
        
        // Pause Game / Freeze Player (MenuController usually handles pause, 
        // but if not, ensure timeScale is 0 here)
        Time.timeScale = 0f;

        // Show Veil & Prompt
        overlay.ShowVeilAndPrompt(true);

        // -- STEP 1: MATERIAL --
        overlay.HighlightButton(materialBtn, spriteMaterialInfo);
        yield return WaitForEnter();

        // -- STEP 2: BROOM --
        overlay.HighlightButton(broomBtn, spriteBroomInfo);
        yield return WaitForEnter();

        // -- STEP 3: RESET --
        overlay.HighlightButton(resetBtn, spriteResetInfo);
        yield return WaitForEnter();

        // -- END SEQUENCE --
        Time.timeScale = 1f; // Unfreeze
        yield return overlay.FadeOutVeil();
    }

    private IEnumerator FinishSequence()
    {
        // Unlock Door
        if (doorTeleport) doorTeleport.UnlockDoor();

        // Slide Animation
        if (roomDoor != null)
        {
            Vector3 start = roomDoor.transform.position;
            Vector3 end = start + Vector3.up * doorSlideHeight;
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                roomDoor.transform.position = Vector3.Lerp(start, end, Mathf.Sin((t/2f) * Mathf.PI * 0.5f));
                yield return null;
            }
            roomDoor.transform.position = end;
        }
    }
    
    // --- LEVEL 2 HELPER ---
    // Call this from a trigger or start if this is Level 2
    public void StartLevel2Instruction()
    {
        StartCoroutine(Level2Routine());
    }

    private IEnumerator Level2Routine()
    {
        // Show "Draw" sprite at top
        overlay.ShowSimpleSprite(spriteDrawInstruction);
        
        // Wait for drawing event (Assuming LineManager exists)
        // You would likely hook into LineManager.OnLineFinished here
        bool drewLine = false;
        System.Action<Drawing.LineControl.Line> action = (l) => drewLine = true;
        
        var lm = FindFirstObjectByType<Drawing.LineControl.LineManager>();
        if (lm) lm.OnLineFinished += action;

        while (!drewLine) yield return null;

        if (lm) lm.OnLineFinished -= action;

        overlay.HideSimpleSprite();
    }

    private IEnumerator WaitForEnter()
    {
        // Wait for press
        while (true)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || 
                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                (submitAction != null && submitAction.action.WasPerformedThisFrame()))
            {
                break;
            }
            yield return null; // Use unscaled time if game is paused
        }
        // Small debounce
        yield return new WaitForSecondsRealtime(0.1f);
    }
}
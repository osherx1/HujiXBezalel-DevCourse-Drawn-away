using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bridges Input Action assets to the characterMovement/characterJump scripts so we can
/// remove the bespoke PlayerController wrapper.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(characterMovement))]
[RequireComponent(typeof(characterJump))]
public class characterInputRelay : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Fallback Keyboard Controls")]
    [SerializeField] private bool enableKeyboardFallback = true;
    [SerializeField] private Key leftKey = Key.A;
    [SerializeField] private Key leftKeySecondary = Key.LeftArrow;
    [SerializeField] private Key rightKey = Key.D;
    [SerializeField] private Key rightKeySecondary = Key.RightArrow;
    [SerializeField] private Key jumpKeyPrimary = Key.Space;
    [SerializeField] private Key jumpKeySecondary = Key.W;
    [SerializeField] private Key jumpKeyTertiary = Key.UpArrow;

    private characterMovement movement;
    private characterJump jump;

    private void Awake()
    {
        movement = GetComponent<characterMovement>();
        jump = GetComponent<characterJump>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed += OnMoveAction;
            moveAction.action.canceled += OnMoveAction;
            moveAction.action.Enable();
        }

        if (jumpAction != null)
        {
            jumpAction.action.started += jump.OnJump;
            jumpAction.action.canceled += jump.OnJump;
            jumpAction.action.Enable();
        }
    }

    private void Update()
    {
        if (!enableKeyboardFallback)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        //If Input Actions are assigned/enabled, don't override them with fallback keyboard.
        bool shouldFallbackMove = moveAction == null || moveAction.action == null || !moveAction.action.enabled;
        bool shouldFallbackJump = jumpAction == null || jumpAction.action == null || !jumpAction.action.enabled;

        if (shouldFallbackMove)
        {
            float input = 0f;
            if (keyboard[leftKey].isPressed || keyboard[leftKeySecondary].isPressed)
            {
                input -= 1f;
            }
            if (keyboard[rightKey].isPressed || keyboard[rightKeySecondary].isPressed)
            {
                input += 1f;
            }
            movement.SetDirectionalInput(Mathf.Clamp(input, -1f, 1f));
        }

        if (shouldFallbackJump)
        {
            bool jumpPressed =
                keyboard[jumpKeyPrimary].wasPressedThisFrame ||
                keyboard[jumpKeySecondary].wasPressedThisFrame ||
                keyboard[jumpKeyTertiary].wasPressedThisFrame;

            bool jumpReleased =
                keyboard[jumpKeyPrimary].wasReleasedThisFrame ||
                keyboard[jumpKeySecondary].wasReleasedThisFrame ||
                keyboard[jumpKeyTertiary].wasReleasedThisFrame;

            if (jumpPressed)
            {
                jump.StartJumpInput();
            }

            if (jumpReleased)
            {
                jump.StopJumpInput();
            }
        }
    }

    private void OnMoveAction(InputAction.CallbackContext context)
    {
        if (movement == null)
        {
            return;
        }

        //Support both float axis actions and Vector2 actions (use X).
        float x;
        if (context.valueType == typeof(Vector2))
        {
            x = context.ReadValue<Vector2>().x;
        }
        else
        {
            x = context.ReadValue<float>();
        }

        movement.SetDirectionalInput(Mathf.Clamp(x, -1f, 1f));
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= OnMoveAction;
            moveAction.action.canceled -= OnMoveAction;
            moveAction.action.Disable();
        }

        if (jumpAction != null)
        {
            jumpAction.action.started -= jump.OnJump;
            jumpAction.action.canceled -= jump.OnJump;
            jumpAction.action.Disable();
        }
    }
}

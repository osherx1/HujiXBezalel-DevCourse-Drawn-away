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
    [SerializeField] private Key rightKey = Key.D;
    [SerializeField] private Key jumpKeyPrimary = Key.Space;
    [SerializeField] private Key jumpKeySecondary = Key.W;

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
            moveAction.action.performed += movement.OnMovement;
            moveAction.action.canceled += movement.OnMovement;
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

        float input = 0f;
        if (keyboard[leftKey].isPressed)
        {
            input -= 1f;
        }
        if (keyboard[rightKey].isPressed)
        {
            input += 1f;
        }
        movement.SetDirectionalInput(Mathf.Clamp(input, -1f, 1f));

        bool jumpPressed = keyboard[jumpKeyPrimary].wasPressedThisFrame || keyboard[jumpKeySecondary].wasPressedThisFrame;
        bool jumpReleased = keyboard[jumpKeyPrimary].wasReleasedThisFrame || keyboard[jumpKeySecondary].wasReleasedThisFrame;

        if (jumpPressed)
        {
            jump.StartJumpInput();
        }

        if (jumpReleased)
        {
            jump.StopJumpInput();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= movement.OnMovement;
            moveAction.action.canceled -= movement.OnMovement;
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

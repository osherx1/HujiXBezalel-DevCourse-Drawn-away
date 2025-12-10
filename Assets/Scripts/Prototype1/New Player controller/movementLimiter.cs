using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Centralized gate that lets other systems temporarily freeze or re-enable player input.
/// Matches the field-based API that the imported controller scripts expect.
/// </summary>
[DisallowMultipleComponent]
public class movementLimiter : MonoBehaviour
{
    [Tooltip("When true the player starts in a frozen state.")]
    [SerializeField] private bool startLocked;

    [Header("State")]
    [Tooltip("Indicates whether the controller may currently read movement inputs.")]
    public bool characterCanMove = true;

    [Header("Events")]
    public UnityEvent onMovementLocked = new UnityEvent();
    public UnityEvent onMovementUnlocked = new UnityEvent();

    private void Awake()
    {
        characterCanMove = !startLocked;
    }

    /// <summary>
    /// Convenience method for freezing the player.
    /// </summary>
    public void LockMovement()
    {
        SetMovementState(false);
    }

    /// <summary>
    /// Convenience method for releasing the player.
    /// </summary>
    public void UnlockMovement()
    {
        SetMovementState(true);
    }

    public void SetMovementState(bool canMove)
    {
        if (characterCanMove == canMove)
        {
            return;
        }

        characterCanMove = canMove;
        if (characterCanMove)
        {
            onMovementUnlocked?.Invoke();
        }
        else
        {
            onMovementLocked?.Invoke();
        }
    }
}

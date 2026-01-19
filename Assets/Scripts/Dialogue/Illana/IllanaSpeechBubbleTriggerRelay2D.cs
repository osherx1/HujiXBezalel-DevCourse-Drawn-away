using UnityEngine;

/// <summary>
/// Forwards 2D trigger callbacks from a child Collider2D GameObject to an owning
/// IllanaSpeechBubbleTrigger component (typically on the parent).
///
/// This is needed when the trigger collider is not on the same GameObject as the
/// IllanaSpeechBubbleTrigger, since Unity only invokes OnTriggerEnter2D/Exit2D on
/// components attached to the collider/rigidbody GameObject.
/// </summary>
[DisallowMultipleComponent]
public sealed class IllanaSpeechBubbleTriggerRelay2D : MonoBehaviour
{
    [SerializeField] private IllanaSpeechBubbleTrigger owner;

    public void SetOwner(IllanaSpeechBubbleTrigger newOwner)
    {
        owner = newOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.RelayTriggerEnter(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        owner?.RelayTriggerExit(other);
    }
}

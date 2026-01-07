using UnityEngine;

/// <summary>
/// Makes a speech bubble follow a world-space target using an offset.
/// Implemented using the same approach as Utilities.TargetPositionTracker (LateUpdate + target.position + offset).
/// </summary>
[DisallowMultipleComponent]
public class SpeechBubbleFollower : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + offset;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
}

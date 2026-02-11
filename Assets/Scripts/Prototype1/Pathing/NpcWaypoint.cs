using UnityEngine;
using UnityEngine.Serialization;

public class NpcWaypoint : MonoBehaviour
{
    [Header("Gizmos")]
    [SerializeField, Min(0.01f)] private float pointGizmoRadius = 0.15f;

    [Header("Behavior")]
    [Tooltip("If enabled, NPC will wait at this waypoint until the player is within the distance below.")]
    [SerializeField] private bool waitForPlayerDistance = true;

    [Tooltip("NPC proceeds when player is within this distance.")]
    [SerializeField, Min(0f)] private float requiredPlayerDistance = 3f;

    [Header("Animation")]
    [Tooltip("Animator Trigger to fire when the NPC arrives at this waypoint. Leave empty for none.")]
    [SerializeField] private string onArriveTrigger;

    [Tooltip("Animator Trigger to fire when the NPC leaves this waypoint. Leave empty for none.")]
    [SerializeField] private string onDepartTrigger;

    [Header("End Options")]
    [Tooltip("If enabled and this waypoint is the final waypoint in the path, the NPC GameObject will be set inactive when the NPC arrives.")]
    [FormerlySerializedAs("deactivatePlayerOnArrive")]
    [SerializeField] private bool deactivateNpcOnArrive;

    [Tooltip("Optional delay before deactivating the NPC at the end waypoint (seconds). Useful to let an arrive animation play.")]
    [SerializeField, Min(0f)] private float deactivateDelaySeconds;

    public bool WaitForPlayerDistance => waitForPlayerDistance;
    public float RequiredPlayerDistance => requiredPlayerDistance;
    public string OnArriveTrigger => onArriveTrigger;
    public string OnDepartTrigger => onDepartTrigger;
    public bool DeactivateNpcOnArrive => deactivateNpcOnArrive;
    public float DeactivateDelaySeconds => deactivateDelaySeconds;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, pointGizmoRadius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!waitForPlayerDistance || requiredPlayerDistance <= 0f)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, requiredPlayerDistance);
    }
}

using UnityEngine;
using UnityEngine.Serialization;

public class NpcWaypoint : MonoBehaviour
{
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

    public bool WaitForPlayerDistance => waitForPlayerDistance;
    public float RequiredPlayerDistance => requiredPlayerDistance;
    public string OnArriveTrigger => onArriveTrigger;
    public string OnDepartTrigger => onDepartTrigger;
    public bool DeactivateNpcOnArrive => deactivateNpcOnArrive;

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

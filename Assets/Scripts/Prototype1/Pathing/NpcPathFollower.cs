using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class NpcPathFollower : MonoBehaviour
{
    private enum State
    {
        Idle,
        Moving,
        Waiting,
        Finished,
    }

    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Events")]
    [SerializeField] private UnityEvent onFinished;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [Tooltip("If enabled, flips localScale.x based on movement direction (2D).")]
    [SerializeField] private bool flipByLocalScaleX = true;

    private NpcPath path;
    private Transform player;
    private int waypointIndex;
    private State state = State.Idle;
    private Vector3 baseLocalScale;
    private bool finishedInvoked;

    private readonly HashSet<string> warnedMissingAnimatorParams = new();

    private const float ArriveEpsilon = 0.02f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        baseLocalScale = transform.localScale;
    }

    public void Begin(NpcPath npcPath, Transform playerTransform)
    {
        path = npcPath;
        player = playerTransform;
        finishedInvoked = false;

        if (path == null || path.Waypoints == null || path.Waypoints.Count == 0)
        {
            SetFinished();
            return;
        }

        waypointIndex = 0;
        transform.position = path.Waypoints[0].transform.position;

        FireTrigger(path.Waypoints[0].OnArriveTrigger);
        ApplyEndActionsIfNeeded(path.Waypoints[0]);

        if (waypointIndex >= path.Waypoints.Count - 1)
        {
            SetFinished();
            return;
        }

        state = ShouldWaitAt(path.Waypoints[0]) ? State.Waiting : State.Moving;
    }

    private void Update()
    {
        if (state == State.Idle || state == State.Finished)
        {
            return;
        }

        if (path == null || path.Waypoints == null || path.Waypoints.Count == 0)
        {
            SetFinished();
            return;
        }

        if (state == State.Waiting)
        {
            var current = path.Waypoints[waypointIndex];
            if (!ShouldWaitAt(current) || IsPlayerCloseEnough(current))
            {
                FireTrigger(current.OnDepartTrigger);

                if (waypointIndex >= path.Waypoints.Count - 1)
                {
                    SetFinished();
                }
                else
                {
                    state = State.Moving;
                }
            }

            return;
        }

        // Moving (continuous): if a waypoint does NOT require waiting, we keep going
        // in the same frame (prevents a visible 1-frame pause).
        float remainingMove = moveSpeed * Time.deltaTime;
        Vector3 position = transform.position;

        while (remainingMove > 0f && state == State.Moving)
        {
            if (waypointIndex >= path.Waypoints.Count - 1)
            {
                SetFinished();
                break;
            }

            var nextWaypoint = path.Waypoints[waypointIndex + 1];
            Vector3 target = nextWaypoint.transform.position;

            Vector3 delta = target - position;
            if (flipByLocalScaleX && Mathf.Abs(delta.x) > 0.001f)
            {
                float sign = delta.x >= 0f ? 1f : -1f;
                transform.localScale = new Vector3(Mathf.Abs(baseLocalScale.x) * sign, baseLocalScale.y, baseLocalScale.z);
            }

            float distanceToTarget = Vector2.Distance(position, target);
            if (distanceToTarget <= Mathf.Max(ArriveEpsilon, 0.0001f))
            {
                position = target;
                waypointIndex++;
            }
            else if (distanceToTarget <= remainingMove)
            {
                position = target;
                remainingMove -= distanceToTarget;
                waypointIndex++;
            }
            else
            {
                position = Vector3.MoveTowards(position, target, remainingMove);
                remainingMove = 0f;
                break;
            }

            var arrived = path.Waypoints[waypointIndex];
            FireTrigger(arrived.OnArriveTrigger);
            ApplyEndActionsIfNeeded(arrived);

            if (waypointIndex >= path.Waypoints.Count - 1)
            {
                SetFinished();
                break;
            }

            if (ShouldWaitAt(arrived))
            {
                state = State.Waiting;
                break;
            }
        }

        transform.position = position;
    }

    private void SetFinished()
    {
        state = State.Finished;
        if (!finishedInvoked)
        {
            finishedInvoked = true;
            onFinished?.Invoke();
        }
    }

    private void ApplyEndActionsIfNeeded(NpcWaypoint waypoint)
    {
        if (path == null || path.Waypoints == null)
        {
            return;
        }

        bool isEnd = waypointIndex >= path.Waypoints.Count - 1;
        if (!isEnd || waypoint == null || !waypoint.DeactivateNpcOnArrive)
        {
            return;
        }

        float delay = Mathf.Max(0f, waypoint.DeactivateDelaySeconds);
        if (delay <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        StartCoroutine(DeactivateAfterSeconds(delay));
    }

    private IEnumerator DeactivateAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }

    private bool ShouldWaitAt(NpcWaypoint waypoint)
    {
        return waypoint != null && waypoint.WaitForPlayerDistance && waypoint.RequiredPlayerDistance > 0f;
    }

    private bool IsPlayerCloseEnough(NpcWaypoint waypoint)
    {
        if (player == null || waypoint == null)
        {
            return true;
        }

        float distance = Vector2.Distance(player.position, waypoint.transform.position);
        return distance <= waypoint.RequiredPlayerDistance;
    }

    private void FireTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        if (!HasAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
        {
            if (warnedMissingAnimatorParams.Add(triggerName))
            {
                Debug.LogWarning($"NpcPathFollower on '{name}': Animator has no Trigger parameter named '{triggerName}'. Check the NPC's Animator Controller.", this);
            }

            return;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.parameters == null)
        {
            return false;
        }

        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.name == paramName)
            {
                return true;
            }
        }

        return false;
    }
}

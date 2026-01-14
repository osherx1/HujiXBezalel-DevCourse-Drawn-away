using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Orchestrates Illana quest flow:
/// 1) Player meets Illana -> bubble variant 0
/// 2) Wait for a LineTouchAllObjective2D to complete
/// 3) Start Illana NpcPathFollower along a path
/// 4) When Illana finishes, show bubble variant 1 and trigger the Dude's NpcPathStartTrigger
/// </summary>
[DisallowMultipleComponent]
public class IllanaLineQuestController : MonoBehaviour
{
    [Header("Progress")]
    [Tooltip("Optional: if empty, uses IllanaProgressManager.Instance.")]
    [SerializeField] private IllanaProgressManager progress;

    [Header("Objective")]
    [SerializeField] private LineTouchAllObjective2D objective;

    [Tooltip("If true, the line objective is ignored until the player has met Illana at least once.")]
    [SerializeField] private bool requireMeetingIllanaBeforeObjective = true;

    [Header("Illana Movement")]
    [SerializeField] private NpcPathFollower illanaFollower;
    [SerializeField] private NpcPath illanaPath;

    [Tooltip("Player transform used for waypoint waiting distances.")]
    [SerializeField] private Transform player;

    [Header("Dialogue")]
    [SerializeField] private SpeechBubbleRegistry registry;
    [SerializeField] private CharacterId character = CharacterId.Illana;

    [Tooltip("Bubble shown while idle / before escort completes.")]
    [SerializeField] private int idleBubbleVariant = 0;

    [Tooltip("Bubble shown when escort completes.")]
    [SerializeField] private int completedBubbleVariant = 1;

    [Tooltip("If true, hides other bubbles when showing Illana's bubble.")]
    [SerializeField] private bool hideOthersOnShow = true;

    [Header("Dude")]
    [Tooltip("Optional: a NpcPathStartTrigger for the Dude. Will be triggered when Illana finishes her path.")]
    [SerializeField] private NpcPathStartTrigger dudePathTrigger;

    [Header("Events")]
    public UnityEvent onObjectiveCompleted;
    public UnityEvent onEscortStarted;
    public UnityEvent onEscortCompleted;

    private bool _startedEscort;

    private void Awake()
    {
        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }

        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        if (illanaFollower == null)
        {
            illanaFollower = GetComponentInChildren<NpcPathFollower>();
            if (illanaFollower == null)
            {
                illanaFollower = GetComponent<NpcPathFollower>();
            }
        }

        UpdateObjectiveEnabled();
    }

    private void OnEnable()
    {
        if (objective != null)
        {
            objective.onAllTouched.AddListener(HandleObjectiveComplete);
        }

        if (progress != null)
        {
            progress.StateChanged += OnProgressStateChanged;
        }

        UpdateObjectiveEnabled();
    }

    private void OnDisable()
    {
        if (objective != null)
        {
            objective.onAllTouched.RemoveListener(HandleObjectiveComplete);
        }

        if (progress != null)
        {
            progress.StateChanged -= OnProgressStateChanged;
        }
    }

    private void Start()
    {
        // Do NOT show Illana's bubble automatically on scene start.
        // The bubble should be shown by IllanaSpeechBubbleTrigger only when the player reaches her.
        UpdateObjectiveEnabled();
    }

    private void OnProgressStateChanged(object sender, IllanaProgressManager.StateChangedEventArgs e)
    {
        UpdateObjectiveEnabled();
    }

    private void UpdateObjectiveEnabled()
    {
        if (objective == null)
        {
            return;
        }

        if (!requireMeetingIllanaBeforeObjective)
        {
            objective.enabled = true;
            return;
        }

        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }

        objective.enabled = (progress != null && progress.HasMet);
    }

    public void RefreshBubble()
    {
        if (registry == null)
        {
            registry = FindObjectOfType<SpeechBubbleRegistry>(true);
        }

        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }

        if (registry == null || progress == null)
        {
            return;
        }

        int variant = progress.EscortCompleted ? completedBubbleVariant : idleBubbleVariant;
        registry.ShowVariant(character, variant, hideOthersOnShow);
    }

    private void HandleObjectiveComplete()
    {
        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }

        if (requireMeetingIllanaBeforeObjective && (progress == null || !progress.HasMet))
        {
            // Objective cannot complete/start until the player has actually met Illana.
            return;
        }

        progress?.MarkObjectiveCompleted();
        onObjectiveCompleted?.Invoke();

        StartEscort();
    }

    public void StartEscort()
    {
        if (_startedEscort)
        {
            return;
        }

        if (progress != null && progress.EscortCompleted)
        {
            return;
        }

        if (illanaFollower == null || illanaPath == null)
        {
            return;
        }

        _startedEscort = true;
        onEscortStarted?.Invoke();

        // We rely on NpcPathFollower's onFinished UnityEvent to call OnIllanaFinished().
        illanaFollower.Begin(illanaPath, player);
    }

    public void OnIllanaFinished()
    {
        if (progress == null)
        {
            progress = IllanaProgressManager.Instance;
        }

        progress?.MarkEscortCompleted();
        onEscortCompleted?.Invoke();

        if (registry != null)
        {
            registry.ShowVariant(character, completedBubbleVariant, hideOthersOnShow);
        }

        if (dudePathTrigger != null)
        {
            dudePathTrigger.TriggerNow(player);
        }
    }
}

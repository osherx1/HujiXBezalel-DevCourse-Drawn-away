using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Watches a list of LineTouchTarget2D and fires when all are touched.
/// </summary>
[DisallowMultipleComponent]
public class LineTouchAllObjective2D : MonoBehaviour
{
    private enum CompletionMode
    {
        AllTargets = 0,
        AtLeastCount = 1,
        AtLeastPercent = 2,
    }

    [Header("Targets")]
    [SerializeField] private List<LineTouchTarget2D> targets = new List<LineTouchTarget2D>();

    [Header("Behavior")]
    [SerializeField] private bool autoFindTargetsInChildren;

    [Tooltip("How this objective decides it is completed.")]
    [SerializeField] private CompletionMode completionMode = CompletionMode.AllTargets;

    [Tooltip("Used when Completion Mode = AtLeastCount.")]
    [SerializeField, Min(1)] private int requiredTouchedCount = 1;

    [Tooltip("Used when Completion Mode = AtLeastPercent. Example: 0.5 = touch 50% of targets.")]
    [SerializeField, Range(0f, 1f)] private float requiredTouchedPercent = 0.5f;

    [Tooltip("If true, the objective triggers only once.")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Events")]
    public UnityEvent onAllTouched;

    [Header("State (Read Only)")]
    [SerializeField] private bool completed;

    public bool IsCompleted => completed;

    private void Awake()
    {
        if (autoFindTargetsInChildren)
        {
            targets = new List<LineTouchTarget2D>(GetComponentsInChildren<LineTouchTarget2D>(true));
        }
    }

    private void Update()
    {
        if (completed && triggerOnce)
        {
            return;
        }

        if (targets == null || targets.Count == 0)
        {
            return;
        }

        int validCount = 0;
        int touchedCount = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            LineTouchTarget2D t = targets[i];
            if (t == null)
            {
                continue;
            }

            validCount++;
            if (t.IsTouched)
            {
                touchedCount++;
            }
        }

        if (validCount <= 0)
        {
            return;
        }

        bool shouldComplete = false;
        switch (completionMode)
        {
            case CompletionMode.AllTargets:
                shouldComplete = touchedCount >= validCount;
                break;
            case CompletionMode.AtLeastCount:
                shouldComplete = touchedCount >= Mathf.Clamp(requiredTouchedCount, 1, validCount);
                break;
            case CompletionMode.AtLeastPercent:
                float required = Mathf.Clamp01(requiredTouchedPercent) * validCount;
                shouldComplete = touchedCount >= Mathf.CeilToInt(required);
                break;
        }

        if (!shouldComplete)
        {
            return;
        }

        completed = true;
        onAllTouched?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Objective/Force Complete")]
    private void EditorForceComplete()
    {
        completed = true;
        onAllTouched?.Invoke();
    }
#endif
}

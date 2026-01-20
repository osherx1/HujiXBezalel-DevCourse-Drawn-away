using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Watches a list of LineTouchTarget2D and fires when all are touched.
/// </summary>
[DisallowMultipleComponent]
public class LineTouchAllObjective2D : MonoBehaviour
{
    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }

    private enum CompletionMode
    {
        AllTargets = 0,
        AtLeastCount = 1,
        AtLeastPercent = 2,
    }

    [System.Serializable]
    private class Route
    {
        public string name;

        [Header("Npc Path (Optional)")]
        [Tooltip("Optional: if set, scripts can use this to choose which NpcPath to run when this route is completed.")]
        public NpcPath npcPath;

        [Tooltip("Targets that belong to this route.")]
        public List<LineTouchTarget2D> targets = new List<LineTouchTarget2D>();

        [Tooltip("If true, overrides Targets by collecting LineTouchTarget2D components under this Objective.")]
        public bool autoFindTargetsInChildren;

        [Tooltip("How this route decides it is completed.")]
        public CompletionMode completionMode = CompletionMode.AllTargets;

        [Tooltip("Used when Completion Mode = AtLeastCount.")]
        [Min(1)] public int requiredTouchedCount = 1;

        [Tooltip("Used when Completion Mode = AtLeastPercent. Example: 0.5 = touch 50% of targets.")]
        [Range(0f, 1f)] public float requiredTouchedPercent = 0.5f;
    }

    [Header("Routes (Optional)")]
    [Tooltip("If set, the objective completes when ANY one route completes. The first completed route wins.")]
    [SerializeField] private List<Route> routes = new List<Route>();

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

    [Tooltip("Invoked with the selected route index when using Routes.")]
    public IntEvent onRouteCompleted;

    [Header("State (Read Only)")]
    [SerializeField] private bool completed;

    [SerializeField] private int selectedRouteIndex = -1;

    public bool IsCompleted => completed;

    /// <summary>
    /// Index of the route that completed first. -1 when not using routes or not completed.
    /// </summary>
    public int SelectedRouteIndex => selectedRouteIndex;

    public NpcPath SelectedNpcPath => GetRouteNpcPath(selectedRouteIndex);

    public NpcPath GetRouteNpcPath(int routeIndex)
    {
        if (routes == null || routes.Count == 0)
        {
            return null;
        }

        if (routeIndex < 0 || routeIndex >= routes.Count)
        {
            return null;
        }

        Route r = routes[routeIndex];
        return r != null ? r.npcPath : null;
    }

    private void Awake()
    {
        TryMigrateLegacyTargetsToSingleRoute();

        // Route mode
        if (routes != null && routes.Count > 0)
        {
            for (int i = 0; i < routes.Count; i++)
            {
                var r = routes[i];
                if (r == null)
                {
                    continue;
                }

                if (r.autoFindTargetsInChildren)
                {
                    r.targets = new List<LineTouchTarget2D>(GetComponentsInChildren<LineTouchTarget2D>(true));
                }
            }

            return;
        }

        // Legacy single-list mode
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

        if (routes != null && routes.Count > 0)
        {
            UpdateRoutes();
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
        selectedRouteIndex = -1;
        onAllTouched?.Invoke();
    }

    private void UpdateRoutes()
    {
        if (routes == null || routes.Count == 0)
        {
            return;
        }

        for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
        {
            Route route = routes[routeIndex];
            if (route == null)
            {
                continue;
            }

            // Quality-of-life: if the user added exactly one route but didn't populate its targets,
            // fall back to legacy Targets so existing setups keep working.
            List<LineTouchTarget2D> routeTargets = route.targets;
            if ((routeTargets == null || routeTargets.Count == 0) && routes.Count == 1 && targets != null && targets.Count > 0)
            {
                routeTargets = targets;
            }

            if (routeTargets == null || routeTargets.Count == 0)
            {
                continue;
            }

            int validCount = 0;
            int touchedCount = 0;

            for (int i = 0; i < routeTargets.Count; i++)
            {
                LineTouchTarget2D t = routeTargets[i];
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
                continue;
            }

            bool shouldComplete = false;
            switch (route.completionMode)
            {
                case CompletionMode.AllTargets:
                    shouldComplete = touchedCount >= validCount;
                    break;
                case CompletionMode.AtLeastCount:
                    shouldComplete = touchedCount >= Mathf.Clamp(route.requiredTouchedCount, 1, validCount);
                    break;
                case CompletionMode.AtLeastPercent:
                    float required = Mathf.Clamp01(route.requiredTouchedPercent) * validCount;
                    shouldComplete = touchedCount >= Mathf.CeilToInt(required);
                    break;
            }

            if (!shouldComplete)
            {
                continue;
            }

            completed = true;
            selectedRouteIndex = routeIndex;
            onAllTouched?.Invoke();
            onRouteCompleted?.Invoke(routeIndex);
            return;
        }
    }

    private void TryMigrateLegacyTargetsToSingleRoute()
    {
        if (routes == null || routes.Count != 1)
        {
            return;
        }

        Route r0 = routes[0];
        if (r0 == null)
        {
            return;
        }

        if (r0.targets != null && r0.targets.Count > 0)
        {
            return;
        }

        if (targets == null || targets.Count == 0)
        {
            return;
        }

        // Copy legacy targets into Route 0 so "Routes" mode behaves immediately.
        r0.targets = new List<LineTouchTarget2D>(targets);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryMigrateLegacyTargetsToSingleRoute();
    }
#endif

#if UNITY_EDITOR
    [ContextMenu("Objective/Force Complete")]
    private void EditorForceComplete()
    {
        completed = true;
        selectedRouteIndex = (routes != null && routes.Count > 0) ? 0 : -1;
        onAllTouched?.Invoke();
        if (selectedRouteIndex >= 0)
        {
            onRouteCompleted?.Invoke(selectedRouteIndex);
        }
    }
#endif
}

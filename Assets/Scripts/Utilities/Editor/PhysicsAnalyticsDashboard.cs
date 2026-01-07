using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Drawing.LineControl;
using Drawing.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utilities.Editor
{
    /// <summary>
    /// Production-grade Editor Window for analyzing Line objects' physics properties in real-time.
    /// Features: Efficient caching, visual data representation, CSV export, filtering, and sorting.
    /// </summary>
    public class PhysicsAnalyticsDashboard : EditorWindow
    {
        // Data structures are now in AnalyticsModels.cs

        #region Instance Cache (Performance Optimization)

        /// <summary>
        /// Instance cache of all active Line objects to avoid expensive FindObjectsOfType calls.
        /// Updated via LineManager events for optimal performance.
        /// </summary>
        private HashSet<Line> cachedLines = new HashSet<Line>();
        private bool cacheInitialized = false;

        /// <summary>
        /// Initialize the cache by scanning the scene once.
        /// </summary>
        private void InitializeCache()
        {
            if (cacheInitialized) return;

            cachedLines.Clear();
            Line[] allLines = FindObjectsByType<Line>(FindObjectsSortMode.None);
            foreach (Line line in allLines)
            {
                if (line != null && line.gameObject.activeInHierarchy)
                {
                    cachedLines.Add(line);
                }
            }

            cacheInitialized = true;
        }

        /// <summary>
        /// Add a line to the cache (called when a new line is created).
        /// </summary>
        private void RegisterLine(Line line)
        {
            if (line != null)
            {
                cachedLines.Add(line);
            }
        }

        /// <summary>
        /// Remove a line from the cache (called when a line is destroyed).
        /// </summary>
        private void UnregisterLine(Line line)
        {
            if (line != null)
            {
                cachedLines.Remove(line);
            }
        }

        /// <summary>
        /// Clear the cache (useful when scene changes).
        /// </summary>
        private void ClearCache()
        {
            cachedLines.Clear();
            cacheInitialized = false;
        }

        /// <summary>
        /// Get all cached lines, filtering out null or inactive ones.
        /// </summary>
        private List<Line> GetCachedLines()
        {
            // Clean up null references (in case objects were destroyed without unregistering)
            cachedLines.RemoveWhere(line => line == null || !line.gameObject.activeInHierarchy);

            // Also clean up null references from the start times dictionary
            var nullKeys = _activeLineStartTimes.Keys.Where(k => k == null || !k.gameObject.activeInHierarchy).ToList();
            foreach (var key in nullKeys)
            {
                _activeLineStartTimes.Remove(key);
            }

            return cachedLines.ToList();
        }

        #endregion

        #region Private Fields

        // Persistent Data (survives assembly reloads and Play Mode changes)
        [SerializeField] private SessionData persistentSessionData = new SessionData();
        [SerializeField] private float sessionStartTime = -1f;
        [SerializeField] private float sessionEndTime = -1f;
        [SerializeField] private bool gameFinished = false;

        // Runtime Data (rebuilt from snapshots + live references)
        private Dictionary<string, SettingIDGroup> _groupedData = new Dictionary<string, SettingIDGroup>();
        private List<OutlierInfo> _allOutliers = new List<OutlierInfo>();
        private List<LineData> _allLineData = new List<LineData>(); // For CSV export

        // UI State
        private Vector2 _scrollPosition;
        private Vector2 _outlierScrollPosition;
        private Vector2 _timelineScrollPosition;
        private bool _showOnlyOutliers = false;
        [SerializeField] private bool _showTimelineView = false;
        [SerializeField] private bool _showSceneOverlay = false; // Toggle for Scene View ghost lines
        private SortMode _sortMode = SortMode.SettingIDAscending;
        private bool _useCache = true; // Toggle between cache and manual scan
        [SerializeField] private bool _autoScanEnabled = false; // Auto-Scan toggle
        private double _lastAutoScanTime = 0.0; // Last time auto-scan was performed
        private const double AUTO_SCAN_INTERVAL = 2.5; // Scan every 2.5 seconds

        // Session Timing
        private bool _sessionActive = false;
        private int _lineCounter = 0; // Sequential counter for line indices
        private Dictionary<Line, float> _activeLineStartTimes = new Dictionary<Line, float>(); // Track when each line started

        // Event subscriptions
        private bool _eventsSubscribed = false;
        private LineManager _lineManager;

        // Chart visualization settings
        private const float CHART_HEIGHT = 40f;
        private const float CHART_PADDING = 5f;

        // Chart interaction state (used in DrawMassDistributionChart)
        private float _hoveredMass = 0f;

        // Self-Debugger state
        [SerializeField] private bool _debugFoldout = false;

        #endregion

        #region Enums

        private enum SortMode
        {
            SettingIDAscending,
            SettingIDDescending,
            CountAscending,
            CountDescending,
            MassAscending,
            MassDescending,
            InkCostAscending,
            InkCostDescending,
            CreationTimeAscending,
            CreationTimeDescending
        }

        #endregion

        #region Menu Item & Window Creation

        [MenuItem("Tools/Physics Analytics Dashboard")]
        public static void ShowWindow()
        {
            PhysicsAnalyticsDashboard window = GetWindow<PhysicsAnalyticsDashboard>("Physics Analytics");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            // AGGRESSIVE INITIALIZATION - Works at any point (Edit Mode, Play Mode start, or mid-game)

            // Step 1: Restore persistent data FIRST (before any other operations)
            if (persistentSessionData != null && persistentSessionData.sessionStartTime >= 0f)
            {
                sessionStartTime = persistentSessionData.sessionStartTime;
                sessionEndTime = persistentSessionData.sessionEndTime;
                gameFinished = persistentSessionData.gameFinished;
            }

            // Step 2: Initialize cache immediately (works in both Edit and Play Mode)
            InitializeCache();

            // Step 3: Find LineManager aggressively (don't wait for events)
            if (_lineManager == null)
            {
                _lineManager = FindFirstObjectByType<LineManager>();
            }

            // Step 4: Subscribe to events (safe even if manager is null)
            SubscribeToEvents();

            // Step 5: Handle current state
            if (Application.isPlaying)
            {
                // Game is running - check if we need to start a session
                if (sessionStartTime < 0f && !gameFinished)
                {
                    // No active session - start one immediately
                    StartNewSession();
                }
                else if (sessionStartTime >= 0f && !gameFinished)
                {
                    // Session exists - mark as active and refresh data
                    _sessionActive = true;
                }

                // Always refresh data when in Play Mode (captures current state)
                RefreshData();
            }
            else
            {
                // Edit Mode - restore from snapshots
                _sessionActive = false;
                RebuildDataFromSnapshots();
            }

            // Step 6: Subscribe to lifecycle events
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate; // Auto-repaint mechanism

            // Step 7: Scene View visualization
            if (_showSceneOverlay)
            {
                SceneView.duringSceneGui += OnSceneGUI;
            }

            // Step 8: Force initial repaint
            Repaint();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate; // Remove auto-repaint

            // Unsubscribe from SceneView
            SceneView.duringSceneGui -= OnSceneGUI;

            // Save persistent data before closing
            SavePersistentData();
        }

        /// <summary>
        /// Auto-repaint mechanism for smooth UI updates without mouse movement.
        /// Also handles periodic Auto-Scan when enabled.
        /// </summary>
        private void OnEditorUpdate()
        {
            // Only repaint if we have an active session or are in Play Mode
            if (Application.isPlaying && _sessionActive && !gameFinished)
            {
                Repaint();
            }

            // Auto-Scan: Periodically scan for new lines that aren't in cache
            if (_autoScanEnabled && Application.isPlaying && !gameFinished)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - _lastAutoScanTime >= AUTO_SCAN_INTERVAL)
                {
                    PerformAutoScan();
                    _lastAutoScanTime = currentTime;
                }
            }
        }

        /// <summary>
        /// Handles Play Mode state changes to preserve data across transitions.
        /// Creates a "Final Snapshot" when exiting Play Mode to ensure all data is captured.
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                // Capture final session end time if session was active
                if (_sessionActive && sessionStartTime >= 0f && sessionEndTime < 0f)
                {
                    sessionEndTime = Time.realtimeSinceStartup;
                }

                // Create final snapshot of all current Line objects before they're destroyed
                CreateFinalSnapshot();

                // Save session data before exiting Play Mode
                SavePersistentData();

                _sessionActive = false;

                // Rebuild from snapshots (GameObjects will be destroyed)
                RebuildDataFromSnapshots();

                // Force repaint to show last session data
                Repaint();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Don't clear data - let OnLineStarted trigger new session if needed
                // This allows viewing last session's data even after entering Play Mode
            }
        }

        /// <summary>
        /// Creates a final snapshot of all active Line objects before Play Mode ends.
        /// Ensures no data is lost when GameObjects are destroyed.
        /// </summary>
        private void CreateFinalSnapshot()
        {
            if (persistentSessionData == null)
            {
                persistentSessionData = new SessionData();
            }

            // Get all current lines (both from cache and manual scan)
            List<Line> allLines = new List<Line>();

            if (_useCache)
            {
                allLines = GetCachedLines();
            }
            else
            {
                Line[] linesArray = FindObjectsByType<Line>(FindObjectsSortMode.None);
                allLines = linesArray.Where(line => line != null && line.gameObject.activeInHierarchy).ToList();
            }

            // Create snapshots for any lines not yet captured
            foreach (Line line in allLines)
            {
                if (line == null) continue;

                float creationTime = sessionStartTime >= 0f
                    ? Time.realtimeSinceStartup - sessionStartTime
                    : 0f;

                // Check if snapshot already exists for this line
                bool exists = persistentSessionData.lineSnapshots.Any(s =>
                    line.rigidBody != null && Mathf.Approximately(s.mass, line.rigidBody.mass) &&
                    Mathf.Approximately(s.inkCost, line.InkCost) &&
                    Mathf.Approximately(s.creationTime, creationTime));

                if (!exists)
                {
                    LineSnapshot snapshot = new LineSnapshot(line, creationTime, _lineCounter++);
                    persistentSessionData.lineSnapshots.Add(snapshot);
                }
            }

            // Update session end time
            if (sessionStartTime >= 0f && sessionEndTime < 0f)
            {
                sessionEndTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Saves current session data to persistent storage.
        /// </summary>
        private void SavePersistentData()
        {
            if (persistentSessionData == null)
            {
                persistentSessionData = new SessionData();
            }

            persistentSessionData.sessionStartTime = sessionStartTime;
            persistentSessionData.sessionEndTime = sessionEndTime;
            persistentSessionData.gameFinished = gameFinished;

            // Update snapshots from current line data
            persistentSessionData.lineSnapshots.Clear();
            foreach (var lineData in _allLineData)
            {
                // Find corresponding snapshot or create new one
                var snapshot = persistentSessionData.lineSnapshots
                    .FirstOrDefault(s => Mathf.Approximately(s.mass, lineData.mass) &&
                                         Mathf.Approximately(s.inkCost, lineData.inkCost) &&
                                         Mathf.Approximately(s.creationTime, lineData.creationTime));

                if (snapshot == null)
                {
                    // Create snapshot from line data (GameObject may be null)
                    snapshot = new LineSnapshot(
                        lineData.settingID,
                        lineData.mass,
                        lineData.inkCost,
                        lineData.creationTime,
                        persistentSessionData.lineSnapshots.Count
                    )
                    {
                        isOutlier = lineData.isOutlier,
                        // Copy advanced metrics
                        length = lineData.length,
                        pointCount = lineData.pointCount,
                        straightness = lineData.straightness,
                        centerOfMass = lineData.centerOfMass,
                        velocityMagnitude = lineData.velocityMagnitude,
                        angularVelocity = lineData.angularVelocity,
                        boundsArea = lineData.boundsArea
                    };
                    persistentSessionData.lineSnapshots.Add(snapshot);
                }
                else
                {
                    // Update existing snapshot with latest data
                    snapshot.isOutlier = lineData.isOutlier;
                    snapshot.length = lineData.length;
                    snapshot.pointCount = lineData.pointCount;
                    snapshot.straightness = lineData.straightness;
                    snapshot.centerOfMass = lineData.centerOfMass;
                    snapshot.velocityMagnitude = lineData.velocityMagnitude;
                    snapshot.angularVelocity = lineData.angularVelocity;
                    snapshot.boundsArea = lineData.boundsArea;
                }
            }
        }

        /// <summary>
        /// Rebuilds analysis data from persistent snapshots (used when GameObjects are destroyed).
        /// </summary>
        private void RebuildDataFromSnapshots()
        {
            if (persistentSessionData == null || persistentSessionData.lineSnapshots == null ||
                persistentSessionData.lineSnapshots.Count == 0)
            {
                return;
            }

            _groupedData.Clear();
            _allOutliers.Clear();
            _allLineData.Clear();

            // Group snapshots by settingID
            var groupedByID = persistentSessionData.lineSnapshots
                .GroupBy(s => s.settingID);

            foreach (var group in groupedByID)
            {
                SettingIDGroup stats = new SettingIDGroup
                {
                    settingID = group.Key,
                    count = group.Count(),
                    snapshots = group.ToList()
                };

                List<float> masses = new List<float>();
                List<float> lengths = new List<float>();
                List<float> straightnesses = new List<float>();
                List<float> velocities = new List<float>();
                float totalInk = 0f;

                foreach (LineSnapshot snapshot in group)
                {
                    masses.Add(snapshot.mass);
                    totalInk += snapshot.inkCost;

                    // Collect advanced metrics for averages
                    if (snapshot.length > 0f) lengths.Add(snapshot.length);
                    if (snapshot.straightness > 0f) straightnesses.Add(snapshot.straightness);
                    if (snapshot.velocityMagnitude > 0f) velocities.Add(snapshot.velocityMagnitude);

                    _allLineData.Add(new LineData
                    {
                        settingID = snapshot.settingID,
                        mass = snapshot.mass,
                        inkCost = snapshot.inkCost,
                        isOutlier = snapshot.isOutlier,
                        creationTime = snapshot.creationTime,
                        length = snapshot.length,
                        pointCount = snapshot.pointCount,
                        straightness = snapshot.straightness,
                        centerOfMass = snapshot.centerOfMass,
                        velocityMagnitude = snapshot.velocityMagnitude,
                        angularVelocity = snapshot.angularVelocity,
                        boundsArea = snapshot.boundsArea
                    });
                }

                if (masses.Count > 0)
                {
                    stats.averageMass = masses.Average();
                    stats.minMass = masses.Min();
                    stats.maxMass = masses.Max();
                }

                stats.totalInkCost = totalInk;

                // Calculate advanced metrics averages
                stats.averageLength = lengths.Count > 0 ? lengths.Average() : 0f;
                stats.averageStraightness = straightnesses.Count > 0 ? straightnesses.Average() : 1f;
                stats.averageVelocity = velocities.Count > 0 ? velocities.Average() : 0f;

                _groupedData[group.Key] = stats;
            }

            // Detect outliers from snapshots
            DetectOutliersFromSnapshots();
        }

        /// <summary>
        /// Detects outliers from persistent snapshots using Z-Score (used when GameObjects are destroyed).
        /// </summary>
        private void DetectOutliersFromSnapshots()
        {
            _allOutliers.Clear();

            foreach (var kvp in _groupedData)
            {
                SettingIDGroup group = kvp.Value;
                group.outlierSnapshots.Clear();

                // Calculate variance and standard deviation from snapshots
                List<float> masses = new List<float>();
                foreach (LineSnapshot snapshot in group.snapshots)
                {
                    if (snapshot != null)
                    {
                        masses.Add(snapshot.mass);
                    }
                }

                if (masses.Count < 2)
                {
                    // Need at least 2 samples for meaningful standard deviation
                    group.variance = 0f;
                    group.standardDeviation = 0f;
                    continue;
                }

                // Calculate variance: Var = E[(X - μ)²]
                float variance = 0f;
                foreach (float mass in masses)
                {
                    float diff = mass - group.averageMass;
                    variance += diff * diff;
                }
                variance /= masses.Count;
                group.variance = variance;

                // Calculate standard deviation: σ = √Var
                group.standardDeviation = Mathf.Sqrt(variance);

                // Z-Score threshold: 2.5 or 3 standard deviations
                const float zScoreThreshold = 2.5f;

                foreach (LineSnapshot snapshot in group.snapshots)
                {
                    if (snapshot == null || group.averageMass <= 0f)
                        continue;

                    // Calculate Z-Score: z = (x - μ) / σ
                    if (group.standardDeviation > 0.0001f)
                    {
                        float zScore = (snapshot.mass - group.averageMass) / group.standardDeviation;

                        // Flag as outlier if Z-Score exceeds threshold
                        if (zScore > zScoreThreshold)
                        {
                            OutlierInfo outlier = new OutlierInfo
                            {
                                line = null, // GameObject may be destroyed
                                snapshot = snapshot,
                                settingID = group.settingID,
                                mass = snapshot.mass,
                                averageMass = group.averageMass,
                                deviationFactor = zScore, // Store Z-Score as deviation factor
                                inkCost = snapshot.inkCost,
                                creationTime = snapshot.creationTime
                            };

                            _allOutliers.Add(outlier);
                            snapshot.isOutlier = true;
                            group.outlierSnapshots.Add(snapshot);
                        }
                    }
                }
            }

            // Sort outliers by deviation factor (Z-Score, most extreme first)
            _allOutliers = _allOutliers.OrderByDescending(o => o.deviationFactor).ToList();
        }

        /// <summary>
        /// Starts a new session (clears old data and resets timer).
        /// </summary>
        private void StartNewSession()
        {
            sessionStartTime = Time.realtimeSinceStartup;
            sessionEndTime = -1f;
            _sessionActive = true;
            gameFinished = false;
            _lineCounter = 0;

            // Clear persistent data for new session
            if (persistentSessionData == null)
            {
                persistentSessionData = new SessionData();
            }
            persistentSessionData.lineSnapshots.Clear();
            persistentSessionData.sessionStartTime = sessionStartTime;
            persistentSessionData.sessionEndTime = -1f;
            persistentSessionData.gameFinished = false;

            // Clear runtime data
            _groupedData.Clear();
            _allOutliers.Clear();
            _allLineData.Clear();
            _activeLineStartTimes.Clear(); // Clear tracked start times for new session
        }

        private void Update()
        {
            // Real-time repaint for active session timer
            if (Application.isPlaying && _sessionActive && !gameFinished)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            // Top Toolbar
            DrawToolbar();

            // Main Content Area
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            EditorGUILayout.Space(8);

            // Session Summary (prominent display when in Edit Mode with data)
            if (!Application.isPlaying && persistentSessionData != null &&
                persistentSessionData.lineSnapshots != null &&
                persistentSessionData.lineSnapshots.Count > 0)
            {
                DrawSessionSummary();
                EditorGUILayout.Space(10);
            }

            // Session Timer Section
            DrawSessionTimer();
            EditorGUILayout.Space(10);

            // Filters & Sorting Section
            DrawFiltersAndSorting();
            EditorGUILayout.Space(10);

            // Statistics Section
            if (_showTimelineView)
            {
                DrawTimelineAnalysis();
            }
            else
            {
                DrawStatistics();
            }
            EditorGUILayout.Space(10);

            // Outliers Section
            DrawOutliers();

            EditorGUILayout.Space(10);

            // Self-Debugger Section
            DrawSelfDebugger();

            EditorGUILayout.EndVertical();

            // Clean up destroyed lines from cache periodically
            if (Event.current.type == EventType.Layout)
            {
                cachedLines.RemoveWhere(line => line == null);
            }
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventsSubscribed) return;

            // Find LineManager once and cache reference
            if (_lineManager == null)
            {
                _lineManager = FindFirstObjectByType<LineManager>();
            }
            if (_lineManager != null)
            {
                _lineManager.OnLineStarted += OnLineStarted;
                _lineManager.OnLineFinished += OnLineFinished;
            }

            // Subscribe to game finished event
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished += OnGameFinished;
                EventManager.Instance.OnBoardReset += OnBoardReset;
                EventManager.Instance.OnEraserActive += OnEraserActive;
                EventManager.Instance.OnEraserInactive += OnEraserInactive;
            }

            // Subscribe to Line destruction events
            Line.onLineDestroyed += OnLineDestroyed;

            _eventsSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_eventsSubscribed) return;

            if (_lineManager != null)
            {
                _lineManager.OnLineStarted -= OnLineStarted;
                _lineManager.OnLineFinished -= OnLineFinished;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameFinished -= OnGameFinished;
                EventManager.Instance.OnBoardReset -= OnBoardReset;
                EventManager.Instance.OnEraserActive -= OnEraserActive;
                EventManager.Instance.OnEraserInactive -= OnEraserInactive;
            }

            Line.onLineDestroyed -= OnLineDestroyed;

            _eventsSubscribed = false;
        }

        private void OnLineStarted(Line line)
        {
            if (line != null)
            {
                RegisterLine(line);

                // Record the start time for this line
                float startTime = Time.realtimeSinceStartup;
                _activeLineStartTimes[line] = startTime;
            }

            // Start new session on first line (if not already started)
            if (sessionStartTime < 0f)
            {
                StartNewSession();
            }
        }

        private void OnLineFinished(Line line)
        {
            if (line != null)
            {
                RegisterLine(line);

                // Retrieve the start time from the dictionary
                float creationTime = 0f;
                if (_activeLineStartTimes.TryGetValue(line, out float startTime))
                {
                    // Calculate creation time based on start time relative to session start
                    if (sessionStartTime >= 0f)
                    {
                        creationTime = startTime - sessionStartTime;
                    }
                    else
                    {
                        // Fallback: use current time if session not started (shouldn't happen)
                        creationTime = Time.realtimeSinceStartup - sessionStartTime;
                    }

                    // Clean up the dictionary entry after use
                    _activeLineStartTimes.Remove(line);
                }
                else
                {
                    // Fallback: if start time not found, use current time (line started before tracking began)
                    creationTime = sessionStartTime >= 0f
                    ? Time.realtimeSinceStartup - sessionStartTime
                    : 0f;
                }

                // Create and save snapshot
                if (persistentSessionData == null)
                {
                    persistentSessionData = new SessionData();
                }

                LineSnapshot snapshot = new LineSnapshot(line, creationTime, _lineCounter++);
                persistentSessionData.lineSnapshots.Add(snapshot);

                // Refresh data when a line is finished (only if using cache)
                if (_useCache)
                {
                    RefreshData();
                }
            }
        }

        private void OnLineDestroyed(string settingID, int inkCost)
        {
            // Note: We can't get the Line reference from this event, so we'll clean up nulls in RefreshData
            // The cache cleanup happens in GetCachedLines()
            // Also clean up null references from the start times dictionary
            _activeLineStartTimes = _activeLineStartTimes
                .Where(kvp => kvp.Key != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // IMPORTANT: Do NOT remove the line's data from persistentSessionData.lineSnapshots
            // The statistics must remain in history even if the GameObject is destroyed
            // This ensures data persistence for analytics even after lines are erased/reset
        }

        /// <summary>
        /// Called when the board is reset (all lines are destroyed).
        /// Ensures historical data is preserved - snapshots are NOT removed.
        /// </summary>
        private void OnBoardReset()
        {
            // Clear the cache of active line references (they're destroyed)
            cachedLines.Clear();
            cacheInitialized = false;

            // Clear active line start times
            _activeLineStartTimes.Clear();

            // IMPORTANT: Do NOT clear persistentSessionData.lineSnapshots
            // Historical statistics must be preserved even after visual destruction
            // This allows analytics to show complete history including deleted lines

            // Refresh data to update UI (will rebuild from snapshots)
            if (Application.isPlaying)
            {
                RefreshData();
            }
            else
            {
                RebuildDataFromSnapshots();
            }

            Repaint();
        }

        /// <summary>
        /// Called when eraser mode is activated.
        /// Lines may be deleted, but their data should remain in analytics.
        /// </summary>
        private void OnEraserActive()
        {
            // Eraser is active - lines may be deleted soon
            // No action needed - data persistence is handled in OnLineDestroyed
        }

        /// <summary>
        /// Called when eraser mode is deactivated.
        /// </summary>
        private void OnEraserInactive()
        {
            // Eraser is inactive - refresh data to update UI
            if (Application.isPlaying)
            {
                RefreshData();
            }
        }

        /// <summary>
        /// Performs periodic auto-scan to detect newly created lines that aren't in cache.
        /// Creates snapshots immediately for any new lines found.
        /// </summary>
        private void PerformAutoScan()
        {
            if (!Application.isPlaying || gameFinished)
                return;

            try
            {
                // Find all active lines in the scene
                Line[] allLines = FindObjectsByType<Line>(FindObjectsSortMode.None);
                int newLinesFound = 0;

                foreach (Line line in allLines)
                {
                    if (line == null || !line.gameObject.activeInHierarchy)
                        continue;

                    // Check if line is already in cache
                    if (cachedLines.Contains(line))
                        continue;

                    // Check if line already has a snapshot (by checking if it's finalized and has been processed)
                    // We'll create a snapshot if the line is finalized but not yet tracked
                    try
                    {
                        // Only create snapshot for finalized lines (lines that have been finished)
                        // We check if the line has been finalized by checking if it has physics components
                        if (line.rigidBody == null)
                            continue; // Line not yet finalized

                        // Check if we already have a snapshot for this line
                        // We can identify by checking if line's creation time matches any snapshot
                        bool alreadyHasSnapshot = false;
                        if (persistentSessionData != null && persistentSessionData.lineSnapshots != null)
                        {
                            // Try to match by mass and settingID (approximate matching)
                            float lineMass = line.rigidBody != null ? line.rigidBody.mass : 0f;
                            string lineSettingID = string.IsNullOrEmpty(line.SettingID) ? "Unknown" : line.SettingID;

                            alreadyHasSnapshot = persistentSessionData.lineSnapshots.Any(s =>
                                s != null &&
                                Mathf.Approximately(s.mass, lineMass) &&
                                s.settingID == lineSettingID &&
                                Mathf.Approximately(s.inkCost, line.InkCost));
                        }

                        if (!alreadyHasSnapshot)
                        {
                            // New line found - create snapshot
                            RegisterLine(line);

                            // Calculate creation time
                            float creationTime = 0f;
                            if (_activeLineStartTimes.TryGetValue(line, out float startTime))
                            {
                                creationTime = sessionStartTime >= 0f
                                    ? startTime - sessionStartTime
                                    : Time.realtimeSinceStartup - sessionStartTime;
                            }
                            else
                            {
                                // Line started before tracking - use current time as fallback
                                creationTime = sessionStartTime >= 0f
                                    ? Time.realtimeSinceStartup - sessionStartTime
                                    : 0f;

                                // Record start time for future reference
                                _activeLineStartTimes[line] = Time.realtimeSinceStartup;
                            }

                            // Create snapshot
                            if (persistentSessionData == null)
                            {
                                persistentSessionData = new SessionData();
                            }

                            LineSnapshot snapshot = new LineSnapshot(line, creationTime, _lineCounter++);
                            persistentSessionData.lineSnapshots.Add(snapshot);
                            newLinesFound++;
                        }
                    }
                    catch (MissingReferenceException)
                    {
                        // Line was destroyed during scan - skip it
                        continue;
                    }
                    catch (System.Exception ex)
                    {
                        // Log error but continue scanning
                        Debug.LogWarning($"PhysicsAnalyticsDashboard: Error during auto-scan for line {line?.name}: {ex.Message}");
                        continue;
                    }
                }

                // Refresh data if new lines were found
                if (newLinesFound > 0)
                {
                    RefreshData();
                    Repaint();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PhysicsAnalyticsDashboard: Error during auto-scan: {ex.Message}");
            }
        }

        private void OnGameFinished()
        {
            // Stop timer exactly at this timestamp
            if (_sessionActive && sessionStartTime >= 0f)
            {
                sessionEndTime = Time.realtimeSinceStartup;
            }
            gameFinished = true;
            _sessionActive = false;

            // Save persistent data
            SavePersistentData();
        }

        #endregion

        #region Data Collection & Analysis

        /// <summary>
        /// Scans for Line objects using either the efficient cache or manual FindObjectsOfType.
        /// Groups them by settingID and calculates statistics.
        /// </summary>
        private void RefreshData()
        {
            _groupedData.Clear();
            _allOutliers.Clear();
            _allLineData.Clear();

            List<Line> allLines;

            if (_useCache)
            {
                // Use efficient cache (O(1) access, updated via events)
                allLines = GetCachedLines();
            }
            else
            {
                // Fallback to manual scan (expensive, but sometimes needed)
                Line[] linesArray = FindObjectsByType<Line>(FindObjectsSortMode.None);
                allLines = linesArray.Where(line => line != null && line.gameObject.activeInHierarchy).ToList();
            }

            // IMPORTANT: Include snapshots from deleted lines for complete historical data
            // This ensures analytics persist even after lines are destroyed
            List<LineSnapshot> allSnapshots = new List<LineSnapshot>();
            if (persistentSessionData != null && persistentSessionData.lineSnapshots != null)
            {
                allSnapshots.AddRange(persistentSessionData.lineSnapshots);
            }

            // Group active lines by settingID
            var groupedByID = allLines
                .GroupBy(line => string.IsNullOrEmpty(line.SettingID) ? "Unknown" : line.SettingID);

            // Also group snapshots by settingID (includes deleted lines)
            var groupedSnapshotsByID = allSnapshots
                .Where(s => s != null)
                .GroupBy(s => string.IsNullOrEmpty(s.settingID) ? "Unknown" : s.settingID);

            // Merge active lines and snapshots by settingID
            var allGroupKeys = groupedByID.Select(g => g.Key)
                .Union(groupedSnapshotsByID.Select(g => g.Key))
                .Distinct();

            // Calculate statistics for each group (including deleted lines from snapshots)
            foreach (var groupKey in allGroupKeys)
            {
                var activeLinesGroup = groupedByID.FirstOrDefault(g => g.Key == groupKey);
                var snapshotsGroup = groupedSnapshotsByID.FirstOrDefault(g => g.Key == groupKey);

                SettingIDGroup stats = new SettingIDGroup
                {
                    settingID = groupKey,
                    count = activeLinesGroup != null ? activeLinesGroup.Count() : 0,
                    lines = activeLinesGroup != null ? activeLinesGroup.ToList() : new List<Line>(),
                    snapshots = snapshotsGroup != null ? snapshotsGroup.ToList() : new List<LineSnapshot>()
                };

                List<float> masses = new List<float>();
                List<float> lengths = new List<float>();
                List<float> straightnesses = new List<float>();
                List<float> velocities = new List<float>();
                float totalInk = 0f;

                // Process active lines
                if (activeLinesGroup != null)
                {
                    foreach (Line line in activeLinesGroup)
                    {
                        try
                        {
                            if (line == null || line.gameObject == null || !line.gameObject.activeInHierarchy)
                                continue;

                            float lineMass = 0f;
                            float lineInkCost = 0f;

                            // Safely access rigidBody
                            try
                            {
                                if (line.rigidBody != null)
                                {
                                    lineMass = line.rigidBody.mass;
                                    masses.Add(lineMass);
                                }
                            }
                            catch
                            {
                                // Rigidbody destroyed or invalid, skip mass
                            }

                            // Safely access InkCost
                            try
                            {
                                lineInkCost = line.InkCost;
                                totalInk += lineInkCost;
                            }
                            catch
                            {
                                // InkCost access failed, skip
                            }

                            // Find corresponding snapshot for creation time, or use tracked start time
                            float creationTime = 0f;

                            // First, try to get creation time from tracked start time (most accurate)
                            if (_activeLineStartTimes.TryGetValue(line, out float startTime))
                            {
                                if (sessionStartTime >= 0f)
                                {
                                    creationTime = startTime - sessionStartTime;
                                }
                            }

                            // If not found in tracked times, try to find in snapshots
                            if (creationTime <= 0f && snapshotsGroup != null)
                            {
                                try
                                {
                                    var snapshot = snapshotsGroup
                                        .FirstOrDefault(s => s != null &&
                                            Mathf.Approximately(s.mass, lineMass) &&
                                            Mathf.Approximately(s.inkCost, lineInkCost));
                                    if (snapshot != null)
                                    {
                                        creationTime = snapshot.creationTime;
                                        // Snapshot already added to stats.snapshots in initialization
                                    }
                                }
                                catch
                                {
                                    // Snapshot lookup failed, continue
                                }
                            }

                            // Calculate advanced metrics for this line (using helper methods)
                            float lineLength = 0f;
                            int pointCount = 0;
                            float straightness = 1f;
                            Vector3 centerOfMass = Vector3.zero;
                            float velocityMagnitude = 0f;
                            float angularVelocity = 0f;
                            float boundsArea = 0f;

                            CalculateLineGeometryMetrics(line, out lineLength, out pointCount, out straightness);
                            CalculateLinePhysicsMetrics(line, out centerOfMass, out velocityMagnitude, out angularVelocity, out boundsArea);

                            // Collect for averages
                            if (lineLength > 0f) lengths.Add(lineLength);
                            if (straightness > 0f) straightnesses.Add(straightness);
                            if (velocityMagnitude > 0f) velocities.Add(velocityMagnitude);

                            // Store individual line data for CSV export
                            _allLineData.Add(new LineData
                            {
                                settingID = stats.settingID,
                                mass = lineMass,
                                inkCost = lineInkCost,
                                isOutlier = false, // Will be updated in DetectOutliers
                                creationTime = creationTime,
                                length = lineLength,
                                pointCount = pointCount,
                                straightness = straightness,
                                centerOfMass = centerOfMass,
                                velocityMagnitude = velocityMagnitude,
                                angularVelocity = angularVelocity,
                                boundsArea = boundsArea
                            });
                        }
                        catch
                        {
                            // Skip this line if any error occurs (object destroyed, missing components, etc.)
                            continue;
                        }
                    }
                }

                // Process snapshots from deleted lines (add to statistics)
                if (snapshotsGroup != null)
                {
                    foreach (LineSnapshot snapshot in snapshotsGroup)
                    {
                        if (snapshot == null) continue;

                        // Add mass from snapshot (for deleted lines)
                        if (snapshot.mass > 0f)
                        {
                            masses.Add(snapshot.mass);
                        }

                        // Add advanced metrics from snapshot
                        if (snapshot.length > 0f) lengths.Add(snapshot.length);
                        if (snapshot.straightness > 0f) straightnesses.Add(snapshot.straightness);
                        if (snapshot.velocityMagnitude > 0f) velocities.Add(snapshot.velocityMagnitude);

                        totalInk += snapshot.inkCost;

                        // Add to line data for CSV export (includes deleted lines)
                        _allLineData.Add(new LineData
                        {
                            settingID = snapshot.settingID,
                            mass = snapshot.mass,
                            inkCost = snapshot.inkCost,
                            isOutlier = snapshot.isOutlier,
                            creationTime = snapshot.creationTime,
                            length = snapshot.length,
                            pointCount = snapshot.pointCount,
                            straightness = snapshot.straightness,
                            centerOfMass = snapshot.centerOfMass,
                            velocityMagnitude = snapshot.velocityMagnitude,
                            angularVelocity = snapshot.angularVelocity,
                            boundsArea = snapshot.boundsArea
                        });
                    }
                }

                // Update count to include deleted lines from snapshots
                stats.count = stats.lines.Count + (snapshotsGroup != null ? snapshotsGroup.Count() : 0);

                if (masses.Count > 0)
                {
                    stats.averageMass = masses.Average();
                    stats.minMass = masses.Min();
                    stats.maxMass = masses.Max();
                }

                stats.totalInkCost = totalInk;

                // Calculate advanced metrics averages
                stats.averageLength = lengths.Count > 0 ? lengths.Average() : 0f;
                stats.averageStraightness = straightnesses.Count > 0 ? straightnesses.Average() : 1f;
                stats.averageVelocity = velocities.Count > 0 ? velocities.Average() : 0f;

                // Preserve foldout state if group already exists
                if (_groupedData.ContainsKey(groupKey))
                {
                    stats.foldoutExpanded = _groupedData[groupKey].foldoutExpanded;
                }

                _groupedData[groupKey] = stats;
            }

            // Detect outliers using Z-Score
            DetectOutliers();

            // Update outlier flags in line data
            foreach (OutlierInfo outlier in _allOutliers)
            {
                LineData lineData = _allLineData.FirstOrDefault(ld =>
                    ld.settingID == outlier.settingID &&
                    Mathf.Approximately(ld.mass, outlier.mass));
                if (lineData != null)
                {
                    lineData.isOutlier = true;
                }
            }

            // Pre-calculate chart data for performance optimization
            PreCalculateChartData();
        }

        /// <summary>
        /// Pre-calculates chart visualization data for all groups to optimize OnGUI performance.
        /// </summary>
        private void PreCalculateChartData()
        {
            foreach (var kvp in _groupedData)
            {
                SettingIDGroup group = kvp.Value;
                ChartData chartData = group.chartData;

                // Clear previous data
                chartData.normalizedHeights.Clear();
                chartData.barColors.Clear();
                chartData.tooltipTexts.Clear();
                chartData.chartLines.Clear();

                if (group.lines.Count == 0 || group.maxMass <= 0f)
                {
                    chartData.isValid = false;
                    continue;
                }

                chartData.maxMass = group.maxMass;
                chartData.isValid = true;

                // Sample lines if too many (limit for performance)
                int maxBars = 50;
                int step = Mathf.Max(1, group.lines.Count / maxBars);

                for (int i = 0; i < group.lines.Count; i += step)
                {
                    Line line = group.lines[i];
                    if (line == null)
                        continue;

                    try
                    {
                        if (line.rigidBody == null)
                            continue;

                        float mass = line.rigidBody.mass;
                        float normalizedHeight = group.maxMass > 0f ? (mass / group.maxMass) : 0f;

                        chartData.normalizedHeights.Add(normalizedHeight);
                        chartData.chartLines.Add(line);

                        // Determine color: Red for outliers, Green for normal
                        Color barColor = group.outlierLines.Contains(line)
                            ? new Color(1f, 0.3f, 0.3f, 1f)
                            : new Color(0.3f, 0.8f, 0.3f, 1f);
                        chartData.barColors.Add(barColor);

                        // Pre-calculate tooltip text
                        chartData.tooltipTexts.Add($"Mass: {mass:F3}");
                    }
                    catch
                    {
                        // Skip invalid lines
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Identifies Line objects with unusual physics values using Z-Score (Standard Deviation).
        /// Calculates variance and standard deviation for each group, then flags outliers
        /// if their mass is more than 2.5 or 3 standard deviations above the mean.
        /// </summary>
        private void DetectOutliers()
        {
            _allOutliers.Clear();

            foreach (var kvp in _groupedData)
            {
                SettingIDGroup group = kvp.Value;
                group.outlierLines.Clear();

                // Calculate variance and standard deviation for this group
                List<float> masses = new List<float>();
                foreach (Line line in group.lines)
                {
                    try
                    {
                        if (line != null && line.rigidBody != null && line.gameObject.activeInHierarchy)
                        {
                            masses.Add(line.rigidBody.mass);
                        }
                    }
                    catch
                    {
                        // Skip destroyed or invalid lines
                        continue;
                    }
                }

                if (masses.Count < 2)
                {
                    // Need at least 2 samples for meaningful standard deviation
                    group.variance = 0f;
                    group.standardDeviation = 0f;
                    continue;
                }

                // Calculate variance: Var = E[(X - μ)²]
                float variance = 0f;
                foreach (float mass in masses)
                {
                    float diff = mass - group.averageMass;
                    variance += diff * diff;
                }
                variance /= masses.Count;
                group.variance = variance;

                // Calculate standard deviation: σ = √Var
                group.standardDeviation = Mathf.Sqrt(variance);

                // Z-Score threshold: 2.5 or 3 standard deviations (configurable)
                const float zScoreThreshold = 2.5f; // Can be adjusted to 3.0f for stricter detection

                foreach (Line line in group.lines)
                {
                    try
                    {
                        if (line == null || line.rigidBody == null || !line.gameObject.activeInHierarchy)
                            continue;

                        float lineMass = line.rigidBody.mass;

                        // Calculate Z-Score: z = (x - μ) / σ
                        if (group.standardDeviation > 0.0001f)
                        {
                            float zScore = (lineMass - group.averageMass) / group.standardDeviation;

                            // Flag as outlier if Z-Score exceeds threshold
                            if (zScore > zScoreThreshold)
                            {
                                // Find snapshot for creation time
                                float creationTime = 0f;
                                LineSnapshot snapshot = null;
                                if (persistentSessionData != null && persistentSessionData.lineSnapshots != null)
                                {
                                    snapshot = persistentSessionData.lineSnapshots
                                        .FirstOrDefault(s => Mathf.Approximately(s.mass, lineMass) &&
                                                             Mathf.Approximately(s.inkCost, line.InkCost));
                                    if (snapshot != null)
                                    {
                                        creationTime = snapshot.creationTime;
                                        snapshot.isOutlier = true;
                                        group.outlierSnapshots.Add(snapshot);
                                    }
                                }

                                OutlierInfo outlier = new OutlierInfo
                                {
                                    line = line,
                                    snapshot = snapshot,
                                    settingID = group.settingID,
                                    mass = lineMass,
                                    averageMass = group.averageMass,
                                    deviationFactor = zScore, // Store Z-Score as deviation factor
                                    inkCost = line.InkCost,
                                    creationTime = creationTime
                                };

                                _allOutliers.Add(outlier);
                                group.outlierLines.Add(line);
                            }
                        }
                    }
                    catch
                    {
                        // Skip destroyed or invalid lines
                        continue;
                    }
                }
            }

            // Sort outliers by deviation factor (Z-Score, most extreme first)
            _allOutliers = _allOutliers.OrderByDescending(o => o.deviationFactor).ToList();
        }

        /// <summary>
        /// Get filtered and sorted groups based on current UI settings.
        /// </summary>
        private IEnumerable<KeyValuePair<string, SettingIDGroup>> GetFilteredAndSortedGroups()
        {
            IEnumerable<KeyValuePair<string, SettingIDGroup>> filtered = _groupedData;

            // Apply filter
            if (_showOnlyOutliers)
            {
                filtered = filtered.Where(kvp => kvp.Value.outlierLines.Count > 0);
            }

            // Apply sorting
            switch (_sortMode)
            {
                case SortMode.SettingIDAscending:
                    return filtered.OrderBy(x => x.Key);
                case SortMode.SettingIDDescending:
                    return filtered.OrderByDescending(x => x.Key);
                case SortMode.CountAscending:
                    return filtered.OrderBy(x => x.Value.count);
                case SortMode.CountDescending:
                    return filtered.OrderByDescending(x => x.Value.count);
                case SortMode.MassAscending:
                    return filtered.OrderBy(x => x.Value.averageMass);
                case SortMode.MassDescending:
                    return filtered.OrderByDescending(x => x.Value.averageMass);
                case SortMode.InkCostAscending:
                    return filtered.OrderBy(x => x.Value.totalInkCost);
                case SortMode.InkCostDescending:
                    return filtered.OrderByDescending(x => x.Value.totalInkCost);
                case SortMode.CreationTimeAscending:
                    return filtered.OrderBy(x => x.Value.snapshots.Count > 0 ? x.Value.snapshots.Min(s => s.creationTime) : 0f);
                case SortMode.CreationTimeDescending:
                    return filtered.OrderByDescending(x => x.Value.snapshots.Count > 0 ? x.Value.snapshots.Max(s => s.creationTime) : 0f);
                default:
                    return filtered.OrderBy(x => x.Key);
            }
        }

        #endregion

        #region UI Drawing

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                padding = new RectOffset(8, 0, 0, 0)
            };
            EditorGUILayout.LabelField("Physics Analytics Dashboard", titleStyle, GUILayout.Width(220));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(1));

            // Refresh Button with Icon
            GUIContent refreshContent = EditorGUIUtility.IconContent("d_Refresh");
            refreshContent.text = "Refresh";
            if (GUILayout.Button(refreshContent, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshData();
            }

            // Export Button with Icon
            GUIContent exportContent = EditorGUIUtility.IconContent("d_SaveAs");
            exportContent.text = "Export CSV";
            if (GUILayout.Button(exportContent, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ExportToCSV();
            }

            // Clear Button
            GUIContent clearContent = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            clearContent.text = "Clear Data";
            if (GUILayout.Button(clearContent, EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                if (EditorUtility.DisplayDialog("Clear Analytics Data",
                    "Are you sure you want to clear all session data? This cannot be undone.",
                    "Clear", "Cancel"))
                {
                    ClearAllData();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(1));

            // Scene Overlay Toggle
            GUIContent sceneOverlayContent = EditorGUIUtility.IconContent("d_SceneViewFx");
            sceneOverlayContent.text = "Show Scene Overlay";
            bool previousOverlayState = _showSceneOverlay;
            _showSceneOverlay = GUILayout.Toggle(_showSceneOverlay, sceneOverlayContent, EditorStyles.toolbarButton, GUILayout.Width(140));

            // Subscribe/Unsubscribe to SceneView when toggle changes
            if (previousOverlayState != _showSceneOverlay)
            {
                if (_showSceneOverlay)
                {
                    SceneView.duringSceneGui += OnSceneGUI;
                }
                else
                {
                    SceneView.duringSceneGui -= OnSceneGUI;
                }
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(1));

            // Cache Toggle
            _useCache = GUILayout.Toggle(_useCache, "Use Cache", EditorStyles.toolbarButton, GUILayout.Width(90));

            if (!_useCache)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠ Performance Warning", EditorStyles.miniLabel, GUILayout.Width(140));
                GUI.color = Color.white;
            }

            EditorGUILayout.Space(5);

            // Auto-Scan Toggle
            GUIContent autoScanContent = EditorGUIUtility.IconContent("d_Search Icon");
            autoScanContent.text = "Auto-Scan";
            bool previousAutoScanState = _autoScanEnabled;
            _autoScanEnabled = GUILayout.Toggle(_autoScanEnabled, autoScanContent, EditorStyles.toolbarButton, GUILayout.Width(100));

            if (_autoScanEnabled && !previousAutoScanState)
            {
                // Auto-Scan was just enabled - reset timer and perform initial scan
                _lastAutoScanTime = EditorApplication.timeSinceStartup;
                if (Application.isPlaying)
                {
                    PerformAutoScan();
                }
            }

            if (_autoScanEnabled)
            {
                GUI.color = new Color(0.3f, 1f, 0.3f); // Light green
                EditorGUILayout.LabelField("● Active", EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws a prominent Session Summary box showing key metrics from the last session.
        /// Only displayed in Edit Mode when session data is available.
        /// </summary>
        private void DrawSessionSummary()
        {
            if (persistentSessionData == null || persistentSessionData.lineSnapshots == null ||
                persistentSessionData.lineSnapshots.Count == 0)
            {
                return;
            }

            // Calculate session metrics
            float sessionDuration = 0f;
            if (persistentSessionData.sessionStartTime >= 0f)
            {
                if (persistentSessionData.sessionEndTime > 0f)
                {
                    sessionDuration = persistentSessionData.sessionEndTime - persistentSessionData.sessionStartTime;
                }
                else
                {
                    // Fallback: use max creation time as duration estimate
                    float maxTime = persistentSessionData.lineSnapshots.Max(s => s.creationTime);
                    sessionDuration = maxTime;
                }
            }

            int totalLines = persistentSessionData.lineSnapshots.Count;
            float totalMass = persistentSessionData.lineSnapshots.Sum(s => s.mass);
            float avgMassPerSecond = sessionDuration > 0f ? (totalMass / sessionDuration) : 0f;

            // Draw prominent summary box
            EditorGUILayout.BeginVertical("box");

            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 4, 8)
            };
            EditorGUILayout.LabelField("📊 Last Session Summary", headerStyle);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Metrics in a flexible grid layout
            EditorGUILayout.BeginHorizontal();

            // Column 1
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                padding = new RectOffset(4, 4, 2, 2)
            };
            GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 4, 2, 2)
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Session Time:", labelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(FormatTime(sessionDuration), valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Lines Created:", labelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(totalLines.ToString(), valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Column 2
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Mass:", labelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(totalMass.ToString("F2"), valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Avg Mass/Second:", labelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(avgMassPerSecond.ToString("F3"), valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private void DrawSessionTimer()
        {
            GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(0, 0, 4, 4)
            };

            EditorGUILayout.LabelField("Session Timer", sectionHeaderStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(70));

            string statusText;
            string durationText;
            Color statusColor = Color.white;

            if (sessionStartTime < 0f)
            {
                statusText = "Not Started";
                durationText = "00:00:00";
                statusColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.4f, 0.4f);
            }
            else if (gameFinished)
            {
                statusText = "Finished";
                float duration = sessionEndTime - sessionStartTime;
                durationText = FormatTime(duration);
                statusColor = new Color(0.8f, 0.4f, 0.4f);
            }
            else if (_sessionActive)
            {
                statusText = "Active";
                float currentDuration = Time.realtimeSinceStartup - sessionStartTime;
                durationText = FormatTime(currentDuration);
                statusColor = new Color(0.4f, 0.8f, 0.4f);
            }
            else
            {
                statusText = "Paused";
                if (sessionEndTime > 0f)
                {
                    float duration = sessionEndTime - sessionStartTime;
                    durationText = FormatTime(duration);
                }
                else
                {
                    durationText = "00:00:00";
                }
                statusColor = new Color(0.8f, 0.8f, 0.4f);
            }

            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Duration:", GUILayout.Width(70));
            GUIStyle durationStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            EditorGUILayout.LabelField(durationText, durationStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }


        private void DrawFiltersAndSorting()
        {
            GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(0, 0, 4, 4)
            };

            EditorGUILayout.LabelField("Filters & Sorting", sectionHeaderStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.Space(4);

            // Toggles on separate rows to prevent overlapping
            EditorGUILayout.BeginHorizontal();
            _showOnlyOutliers = EditorGUILayout.Toggle("Show Only Outliers", _showOnlyOutliers);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _showTimelineView = EditorGUILayout.Toggle("Show Timeline", _showTimelineView);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sort By:", GUILayout.Width(70));
            _sortMode = (SortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(0, 0, 4, 4)
            };

            EditorGUILayout.LabelField("Statistics by Setting ID", sectionHeaderStyle);

            // Wrap in helpBox container to prevent text overlap
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_groupedData.Count == 0)
            {
                EditorGUILayout.HelpBox("No Line objects found in the scene.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Use flexible scroll view that adapts to window size
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            var filteredGroups = GetFilteredAndSortedGroups().ToList();

            if (filteredGroups.Count == 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("No groups match the current filter settings.", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical(); // Close helpBox container
                return;
            }

            int groupIndex = 0;
            foreach (var kvp in filteredGroups)
            {
                SettingIDGroup group = kvp.Value;

                // Zebra striping - alternate background colors
                Color backgroundColor = EditorGUIUtility.isProSkin
                    ? (groupIndex % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f))
                    : (groupIndex % 2 == 0 ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f));

                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = backgroundColor;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(2);

                // Header with foldout
                EditorGUILayout.BeginHorizontal();

                // Foldout - limit width to prevent text overlap
                string foldoutText = $"Setting ID: {group.settingID}";

                // Use a custom style that clips text if too long
                GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    clipping = TextClipping.Clip
                };

                // Calculate max width for foldout (reserve space for other elements)
                float maxFoldoutWidth = EditorGUIUtility.currentViewWidth > 0
                    ? Mathf.Min(EditorGUIUtility.currentViewWidth * 0.5f, 300f) // Max 50% of width or 300px
                    : 300f;

                // Create a rect for the foldout with limited width
                Rect foldoutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                foldoutRect.width = Mathf.Min(foldoutRect.width, maxFoldoutWidth);
                group.foldoutExpanded = EditorGUI.Foldout(foldoutRect, group.foldoutExpanded, foldoutText, true, foldoutStyle);

                // Add small spacing
                EditorGUILayout.Space(8);

                GUIStyle countStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(4, 0, 0, 0)
                };
                EditorGUILayout.LabelField($"({group.count} lines)", countStyle, GUILayout.ExpandWidth(false));

                if (group.outlierLines.Count > 0)
                {
                    EditorGUILayout.Space(8);
                    GUI.color = new Color(1f, 0.8f, 0.2f);
                    EditorGUILayout.LabelField($"⚠ {group.outlierLines.Count} Outliers", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                    GUI.color = Color.white;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (group.foldoutExpanded)
                {
                    EditorGUILayout.Space(6);

                    // Statistics Table
                    DrawStatisticsTable(group);

                    EditorGUILayout.Space(8);

                    // Visual Mass Distribution Chart
                    AnalyticsChartDrawer.DrawMassDistributionChart(group, position.width, () => Repaint());
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);

                groupIndex++;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical(); // Close helpBox container
        }

        /// <summary>
        /// Draws a neat table structure for statistics.
        /// </summary>
        private void DrawStatisticsTable(SettingIDGroup group)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                padding = new RectOffset(4, 4, 2, 2)
            };

            GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 4, 2, 2)
            };

            // Table rows with flexible layout
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Count:", labelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(group.count.ToString(), valueStyle);
            EditorGUILayout.EndHorizontal();

            if (group.count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Average Mass:", labelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(group.averageMass.ToString("F3"), valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mass Range:", labelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{group.minMass:F3} - {group.maxMass:F3}", valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Total Ink Cost:", labelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(group.totalInkCost.ToString("F1"), valueStyle);
                EditorGUILayout.EndHorizontal();

                // Advanced Metrics
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Advanced Metrics:", labelStyle);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Avg Length:", labelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(group.averageLength.ToString("F2"), valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Avg Straightness:", labelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(group.averageStraightness.ToString("F3"), valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Avg Velocity:", labelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(group.averageVelocity.ToString("F3"), valueStyle);
                EditorGUILayout.EndHorizontal();
            }
        }

        // Chart drawing methods are now in AnalyticsChartDrawer.cs
        // Removed: DrawMassDistributionChart, DrawTimelineHistogram, and all chart helper methods


        // Chart helper methods are now in AnalyticsChartDrawer.cs

        /// <summary>
        /// Clears all session data (both persistent and runtime).
        /// </summary>
        private void ClearAllData()
        {
            StartNewSession();
            _groupedData.Clear();
            _allOutliers.Clear();
            _allLineData.Clear();
            Repaint();
        }

        /// <summary>
        /// Draws Timeline Analysis view - groups lines by creation time.
        /// </summary>
        private void DrawTimelineAnalysis()
        {
            GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(0, 0, 4, 4)
            };

            EditorGUILayout.LabelField("Timeline Analysis", sectionHeaderStyle);

            // Wrap in helpBox container to prevent text overlap
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (persistentSessionData == null || persistentSessionData.lineSnapshots == null ||
                persistentSessionData.lineSnapshots.Count == 0)
            {
                EditorGUILayout.HelpBox("No timeline data available. Start a session to see chronological analysis.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Calculate optimal bucket size based on session duration
            float maxTime = persistentSessionData.lineSnapshots.Max(s => s.creationTime);
            float bucketSize = Mathf.Max(5f, Mathf.Ceil(maxTime / 20f)); // Aim for ~20 buckets max, minimum 5s

            // Group snapshots by time buckets
            var timeBuckets = persistentSessionData.lineSnapshots
                .OrderBy(s => s.creationTime)
                .GroupBy(s => Mathf.FloorToInt(s.creationTime / bucketSize))
                .OrderBy(g => g.Key)
                .ToList();

            // Draw Timeline Histogram (Total Mass over Time)
            AnalyticsChartDrawer.DrawTimelineHistogram(timeBuckets, bucketSize, maxTime, position.width, FormatTime);

            EditorGUILayout.Space(10);

            // Use flexible scroll view that adapts to window size
            _timelineScrollPosition = EditorGUILayout.BeginScrollView(_timelineScrollPosition, GUILayout.ExpandHeight(true));

            int bucketIndex = 0;
            foreach (var bucket in timeBuckets)
            {
                float startTime = bucket.Key * bucketSize;
                float endTime = startTime + bucketSize;
                int lineCount = bucket.Count();

                // Bucket header
                EditorGUILayout.BeginVertical("box");
                Color backgroundColor = EditorGUIUtility.isProSkin
                    ? (bucketIndex % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f))
                    : (bucketIndex % 2 == 0 ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f));

                GUI.backgroundColor = backgroundColor;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                // Calculate bucket statistics
                float avgMass = bucket.Average(s => s.mass);
                float totalMass = bucket.Sum(s => s.mass);
                float totalInk = bucket.Sum(s => s.inkCost);
                int outlierCount = bucket.Count(s => s.isOutlier);

                EditorGUILayout.BeginHorizontal();
                GUIStyle bucketHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };
                EditorGUILayout.LabelField($"{FormatTime(startTime)} - {FormatTime(endTime)}", bucketHeaderStyle, GUILayout.Width(150));
                EditorGUILayout.LabelField($"({lineCount} lines)", EditorStyles.miniLabel, GUILayout.Width(80));

                // Show bucket statistics
                EditorGUILayout.LabelField($"Avg Mass: {avgMass:F2}", EditorStyles.miniLabel, GUILayout.Width(90));
                EditorGUILayout.LabelField($"Total Mass: {totalMass:F2}", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField($"Ink: {totalInk:F1}", EditorStyles.miniLabel, GUILayout.Width(70));

                if (outlierCount > 0)
                {
                    GUI.color = new Color(1f, 0.6f, 0.2f);
                    EditorGUILayout.LabelField($"⚠ {outlierCount} outliers", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // List lines in this bucket
                foreach (LineSnapshot snapshot in bucket.OrderBy(s => s.creationTime))
                {
                    EditorGUILayout.BeginHorizontal();

                    // Line index and time
                    GUIStyle lineStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 10
                    };
                    string timeStr = FormatTime(snapshot.creationTime);
                    EditorGUILayout.LabelField($"Line #{snapshot.lineIndex}", lineStyle, GUILayout.Width(70));
                    EditorGUILayout.LabelField($"(Time: {timeStr})", lineStyle, GUILayout.Width(100));

                    // Mass
                    EditorGUILayout.LabelField($"Mass: {snapshot.mass:F3}", lineStyle, GUILayout.Width(90));

                    // Geometry Metrics (Length and Velocity as requested)
                    EditorGUILayout.LabelField($"Len: {snapshot.length:F2}", lineStyle, GUILayout.Width(70));
                    EditorGUILayout.LabelField($"Vel: {snapshot.velocityMagnitude:F2}", lineStyle, GUILayout.Width(70));

                    // Setting ID
                    EditorGUILayout.LabelField($"ID: {snapshot.settingID}", lineStyle, GUILayout.Width(100));

                    // Outlier indicator
                    if (snapshot.isOutlier)
                    {
                        GUI.color = new Color(1f, 0.6f, 0.2f);
                        EditorGUILayout.LabelField("⚠ Outlier", lineStyle);
                        GUI.color = Color.white;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);

                bucketIndex++;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical(); // Close helpBox container
        }



        private void DrawOutliers()
        {
            GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(0, 0, 4, 4)
            };

            EditorGUILayout.LabelField("Outlier Detection", sectionHeaderStyle);

            // Wrap in helpBox container to prevent text overlap
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_allOutliers.Count == 0)
            {
                EditorGUILayout.HelpBox("No outliers detected. All lines have normal mass values.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.HelpBox(
                $"Found {_allOutliers.Count} outlier(s) with mass > 3x average for their group.",
                MessageType.Warning);

            EditorGUILayout.Space(4);

            // Use flexible scroll view
            _outlierScrollPosition = EditorGUILayout.BeginScrollView(_outlierScrollPosition, GUILayout.ExpandHeight(true));

            int outlierIndex = 0;
            foreach (OutlierInfo outlier in _allOutliers)
            {
                // Zebra striping for outliers too
                Color backgroundColor = EditorGUIUtility.isProSkin
                    ? (outlierIndex % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f))
                    : (outlierIndex % 2 == 0 ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f));

                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = backgroundColor;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(4);

                // Header row
                EditorGUILayout.BeginHorizontal();
                GUIStyle settingIDStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField($"Setting ID: {outlier.settingID}", settingIDStyle, GUILayout.Width(150));

                // Only show Select button if Line GameObject still exists
                if (outlier.line != null && outlier.line.gameObject != null)
                {
                    if (GUILayout.Button("Select", GUILayout.Width(70), GUILayout.Height(20)))
                    {
                        Selection.activeGameObject = outlier.line.gameObject;
                        EditorGUIUtility.PingObject(outlier.line.gameObject);
                    }
                }
                else if (outlier.snapshot != null)
                {
                    // Show snapshot info when GameObject is destroyed
                    EditorGUILayout.LabelField($"[Snapshot: {outlier.snapshot.gameObjectName}]", EditorStyles.miniLabel, GUILayout.Width(120));
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Data row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mass:", GUILayout.Width(60));
                GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField(outlier.mass.ToString("F3"), valueStyle, GUILayout.Width(70));

                EditorGUILayout.LabelField("(Avg:", GUILayout.Width(40));
                EditorGUILayout.LabelField(outlier.averageMass.ToString("F3") + ")", GUILayout.Width(70));

                GUI.color = new Color(1f, 0.6f, 0.2f);
                EditorGUILayout.LabelField($"{outlier.deviationFactor:F1}x", valueStyle, GUILayout.Width(50));
                GUI.color = Color.white;

                EditorGUILayout.LabelField("Ink:", GUILayout.Width(35));
                EditorGUILayout.LabelField(outlier.inkCost.ToString("F1"), valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);

                outlierIndex++;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical(); // Close helpBox container
        }

        #endregion

        #region CSV Export

        /// <summary>
        /// Exports all line data to a timestamped CSV file using AnalyticsCsvExporter.
        /// </summary>
        private void ExportToCSV()
        {
            AnalyticsCsvExporter.ExportToCSV(_allLineData);
        }

        #endregion

        #region Scene View Visualization

        /// <summary>
        /// Draws "Ghost Lines" in the Scene View showing the last session's line geometry.
        /// Called via SceneView.duringSceneGui delegate when _showSceneOverlay is enabled.
        /// Uses color coding: Green = Normal, Red = Outlier, Orange = High Mass.
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showSceneOverlay)
                return;

            if (persistentSessionData == null || persistentSessionData.lineSnapshots == null ||
                persistentSessionData.lineSnapshots.Count == 0)
                return;

            // Calculate average mass for color normalization (if needed)
            float avgMass = persistentSessionData.lineSnapshots.Average(s => s.mass);
            float maxMass = persistentSessionData.lineSnapshots.Max(s => s.mass);

            // Cache colors outside the loop for performance
            Color normalColor = new Color(0.3f, 0.8f, 0.3f, 0.7f); // Green for normal lines
            Color outlierColor = new Color(1f, 0.3f, 0.3f, 0.8f); // Red for outliers
            Color highMassColor = new Color(0.8f, 0.6f, 0.2f, 0.7f); // Orange for high mass

            const float lineThickness = 4.0f;

            // Draw each line snapshot
            foreach (LineSnapshot snapshot in persistentSessionData.lineSnapshots)
            {
                if (snapshot == null || snapshot.worldPoints == null || snapshot.worldPoints.Length < 2)
                    continue;

                // Determine color based on mass and outlier status (same logic as charts)
                Color lineColor = normalColor;

                if (snapshot.isOutlier)
                {
                    // Outliers are always red
                    lineColor = outlierColor;
                }
                else if (snapshot.mass > avgMass * 1.5f)
                {
                    // High mass (above 1.5x average) gets orange color
                    lineColor = highMassColor;
                }
                // Normal lines use green (default)

                // Draw the line using Handles.DrawAAPolyLine for smooth anti-aliased rendering
                Handles.color = lineColor;
                Handles.DrawAAPolyLine(lineThickness, snapshot.worldPoints);
            }

            // Force repaint to keep lines visible during camera movement
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
            {
                SceneView.RepaintAll();
            }
        }

        #endregion

        #region Self-Debugger

        /// <summary>
        /// Draws the internal debugger section for troubleshooting.
        /// </summary>
        private void DrawSelfDebugger()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "[ INTERNAL DEBUG ]", true, EditorStyles.foldoutHeader);

            if (_debugFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space(4);

                // State Variables
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Session Active:", GUILayout.Width(150));
                EditorGUILayout.LabelField(_sessionActive ? "True" : "False", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cache Count:", GUILayout.Width(150));
                EditorGUILayout.LabelField(cachedLines.Count.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cache Initialized:", GUILayout.Width(150));
                EditorGUILayout.LabelField(cacheInitialized ? "Yes" : "No", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Game Time:", GUILayout.Width(150));
                float gameTime = Application.isPlaying ? Time.realtimeSinceStartup : 0f;
                EditorGUILayout.LabelField(gameTime.ToString("F2"), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Manager Hooked:", GUILayout.Width(150));
                EditorGUILayout.LabelField(_lineManager != null ? "Yes" : "No", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Events Subscribed:", GUILayout.Width(150));
                EditorGUILayout.LabelField(_eventsSubscribed ? "Yes" : "No", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Session Start Time:", GUILayout.Width(150));
                EditorGUILayout.LabelField(sessionStartTime >= 0f ? sessionStartTime.ToString("F2") : "Not Started", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Session End Time:", GUILayout.Width(150));
                EditorGUILayout.LabelField(sessionEndTime >= 0f ? sessionEndTime.ToString("F2") : "Not Ended", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Game Finished:", GUILayout.Width(150));
                EditorGUILayout.LabelField(gameFinished ? "True" : "False", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Snapshots Count:", GUILayout.Width(150));
                int snapshotCount = persistentSessionData != null && persistentSessionData.lineSnapshots != null
                    ? persistentSessionData.lineSnapshots.Count
                    : 0;
                EditorGUILayout.LabelField(snapshotCount.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(8);

                // Force Reset Button
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Reset", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Force Reset",
                        "This will completely wipe all state and rescan the scene manually. Continue?",
                        "Reset", "Cancel"))
                    {
                        ForceReset();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Completely wipes state and rescans the scene manually.
        /// </summary>
        private void ForceReset()
        {
            // Clear all state
            cachedLines.Clear();
            cacheInitialized = false;
            _groupedData.Clear();
            _allOutliers.Clear();
            _allLineData.Clear();
            _sessionActive = false;
            sessionStartTime = -1f;
            sessionEndTime = -1f;
            gameFinished = false;
            _lineCounter = 0;

            // Clear persistent data
            if (persistentSessionData != null)
            {
                persistentSessionData.lineSnapshots.Clear();
                persistentSessionData.sessionStartTime = -1f;
                persistentSessionData.sessionEndTime = -1f;
                persistentSessionData.gameFinished = false;
            }

            // Unsubscribe and resubscribe
            UnsubscribeFromEvents();
            SubscribeToEvents();

            // Reinitialize cache
            InitializeCache();

            // Find LineManager again
            _lineManager = FindFirstObjectByType<LineManager>();

            // Refresh data
            if (Application.isPlaying)
            {
                RefreshData();
            }
            else
            {
                RebuildDataFromSnapshots();
            }

            // Force repaint
            Repaint();

            Debug.Log("PhysicsAnalyticsDashboard: Force Reset completed.");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Formats time in seconds to MM:SS:MS format.
        /// </summary>
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int milliseconds = Mathf.FloorToInt((seconds % 1f) * 100f);
            return $"{minutes:00}:{secs:00}:{milliseconds:00}";
        }

        /// <summary>
        /// Calculates geometry metrics for a Line (helper method for RefreshData).
        /// Uses centralized LineAnalyzerUtils to eliminate code duplication.
        /// </summary>
        private void CalculateLineGeometryMetrics(Line line, out float length, out int pointCount, out float straightness)
        {
            LineAnalyzerUtils.CalculateGeometryMetrics(line, out length, out pointCount, out straightness);
        }

        /// <summary>
        /// Calculates physics metrics for a Line (helper method for RefreshData).
        /// Uses centralized LineAnalyzerUtils to eliminate code duplication.
        /// </summary>
        private void CalculateLinePhysicsMetrics(Line line, out Vector3 centerOfMass, out float velocityMagnitude,
            out float angularVelocity, out float boundsArea)
        {
            LineAnalyzerUtils.CalculatePhysicsMetrics(line, out centerOfMass, out velocityMagnitude, out angularVelocity, out boundsArea);
        }

        #endregion
    }
}

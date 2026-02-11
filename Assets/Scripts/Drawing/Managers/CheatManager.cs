using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Drawing.Managers
{
    /// <summary>
    /// Developer utility for scene navigation and player teleportation between waypoints.
    /// Uses the New Input System (Keyboard) for debug bindings: F1/F2 scenes, F5/F6 teleport.
    /// </summary>
    public class CheatManager : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [SerializeField] [Tooltip("If true, Next/Previous scene wrap around build list; otherwise clamp.")]
        private bool loopScenes = true;

        [Header("Waypoints & Player")]
        [SerializeField] [Tooltip("Checkpoints or points of interest; sorted by X position at runtime.")]
        private List<Transform> waypoints = new List<Transform>();

        [SerializeField] [Tooltip("Player transform to teleport.")]
        private Transform player;

        [Header("Feature Toggles")]
        [SerializeField] [Tooltip("If false, teleport actions do nothing.")]
        private bool enableWaypoints = true;

        [SerializeField] [Tooltip("If true, log scene/teleport actions to the console.")]
        private bool showDebugLogs = true;

        [Header("Input (Optional)")]
        [SerializeField] [Tooltip("Override: Next Scene. If unset, defaults to F2.")]
        private InputActionReference nextSceneAction;

        [SerializeField] [Tooltip("Override: Previous Scene. If unset, defaults to F1.")]
        private InputActionReference prevSceneAction;

        [SerializeField] [Tooltip("Override: Reload Scene. If unset, defaults to F3.")]
        private InputActionReference reloadSceneAction;

        [SerializeField] [Tooltip("Override: Next Waypoint. If unset, defaults to F6.")]
        private InputActionReference nextPointAction;

        [SerializeField] [Tooltip("Override: Previous Waypoint. If unset, defaults to F5.")]
        private InputActionReference prevPointAction;

        [Header("Debug Visuals")]
        [SerializeField] [Tooltip("Color of waypoint gizmos in Scene view.")]
        private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);

        [SerializeField] [Tooltip("Radius of wire sphere drawn at each waypoint.")]
        private float gizmoRadius = 0.5f;

        [SerializeField] [Tooltip("If true, draw waypoint name/index above each point in Scene view (Editor only).")]
        private bool showLabels = true;

        /// <summary>Sorted waypoints by X (ascending). Populated in Awake after filtering nulls.</summary>
        private List<Transform> _sortedWaypoints = new List<Transform>();

        /// <summary>Current waypoint index after last teleport; used for optional display. -1 when none or list empty.</summary>
        private int _currentWaypointIndex = -1;

        private void Awake()
        {
            SortAndCacheWaypoints();
        }

        private void OnEnable()
        {
            if (nextSceneAction?.action != null) nextSceneAction.action.Enable();
            if (prevSceneAction?.action != null) prevSceneAction.action.Enable();
            if (reloadSceneAction?.action != null) reloadSceneAction.action.Enable();
            if (nextPointAction?.action != null) nextPointAction.action.Enable();
            if (prevPointAction?.action != null) prevPointAction.action.Enable();
        }

        private void OnDisable()
        {
            if (nextSceneAction?.action != null) nextSceneAction.action.Disable();
            if (prevSceneAction?.action != null) prevSceneAction.action.Disable();
            if (reloadSceneAction?.action != null) reloadSceneAction.action.Disable();
            if (nextPointAction?.action != null) nextPointAction.action.Disable();
            if (prevPointAction?.action != null) prevPointAction.action.Disable();
        }

        private void Update()
        {
            // Scene navigation
            if (WasActionPerformed(prevSceneAction, () => Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame))
                LoadPreviousScene();
            if (WasActionPerformed(nextSceneAction, () => Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame))
                LoadNextScene();
            if (WasActionPerformed(reloadSceneAction, () => Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame))
                ReloadCurrentScene();

            // Teleport (only when enableWaypoints is true)
            if (WasActionPerformed(prevPointAction, () => Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame))
                TryTeleportToPreviousPoint();
            if (WasActionPerformed(nextPointAction, () => Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame))
                TryTeleportToNextPoint();
        }

        /// <summary>True if the optional action was performed this frame, or if action is null and fallback returns true.</summary>
        private static bool WasActionPerformed(InputActionReference actionRef, System.Func<bool> fallback)
        {
            if (actionRef != null && actionRef.action != null)
                return actionRef.action.WasPerformedThisFrame();
            return fallback();
        }

        /// <summary>
        /// Builds and sorts the waypoint list by world X position (ascending).
        /// Null entries are filtered out. Result is cached in _sortedWaypoints.
        /// </summary>
        private void SortAndCacheWaypoints()
        {
            _sortedWaypoints.Clear();
            if (waypoints == null)
                return;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                    _sortedWaypoints.Add(waypoints[i]);
            }

            // Sort by X position so "next" is right, "previous" is left (typical 2D left-to-right level).
            _sortedWaypoints.Sort((a, b) => a.position.x.CompareTo(b.position.x));

            // Initialize current index from player position if we have a valid player and list.
            RefreshCurrentWaypointIndex();
        }

        /// <summary>
        /// Updates _currentWaypointIndex to the waypoint whose X is <= player X, or 0 / count-1 as fallback.
        /// </summary>
        private void RefreshCurrentWaypointIndex()
        {
            if (_sortedWaypoints.Count == 0 || player == null)
            {
                _currentWaypointIndex = -1;
                return;
            }

            float playerX = player.position.x;
            _currentWaypointIndex = 0;
            for (int i = 0; i < _sortedWaypoints.Count; i++)
            {
                if (_sortedWaypoints[i].position.x <= playerX)
                    _currentWaypointIndex = i;
                else
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null)
                return;

            Gizmos.color = gizmoColor;
            for (int i = 0; i < waypoints.Count; i++)
            {
                Transform wp = waypoints[i];
                if (wp == null)
                    continue;

                Vector3 pos = wp.position;
                Gizmos.DrawWireSphere(pos, gizmoRadius);

#if UNITY_EDITOR
                if (showLabels)
                    UnityEditor.Handles.Label(pos + Vector3.up * (gizmoRadius + 0.3f), $"{i}: {wp.name}");
#endif
            }
        }

        // ---------- Scene management ----------

        /// <summary>Loads the next scene in build settings. Wraps to 0 or clamps based on loopScenes.</summary>
        public void LoadNextScene()
        {
            int count = SceneManager.sceneCountInBuildSettings;
            if (count == 0)
                return;

            int current = SceneManager.GetActiveScene().buildIndex;
            int next = current + 1;
            if (next >= count)
                next = loopScenes ? 0 : count - 1;
            if (showDebugLogs)
                Debug.Log($"[CheatManager] Loading Next Scene (build index {next}).");
            SceneManager.LoadScene(next);
        }

        /// <summary>Loads the previous scene in build settings. Wraps to last or clamps based on loopScenes.</summary>
        public void LoadPreviousScene()
        {
            int count = SceneManager.sceneCountInBuildSettings;
            if (count == 0)
                return;

            int current = SceneManager.GetActiveScene().buildIndex;
            int prev = current - 1;
            if (prev < 0)
                prev = loopScenes ? count - 1 : 0;
            if (showDebugLogs)
                Debug.Log($"[CheatManager] Loading Previous Scene (build index {prev}).");
            SceneManager.LoadScene(prev);
        }

        /// <summary>Reloads the currently active scene by build index.</summary>
        public void ReloadCurrentScene()
        {
            int index = SceneManager.GetActiveScene().buildIndex;
            if (showDebugLogs)
                Debug.Log($"[CheatManager] Reloading Current Scene (build index {index}).");
            SceneManager.LoadScene(index);
        }

        // ---------- Teleportation ----------

        /// <summary>
        /// Returns the index in _sortedWaypoints of the waypoint nearest to the player (by distance).
        /// Ensures we always step from a known "current" point, avoiding stuck-on-same-point behavior.
        /// </summary>
        private int GetClosestWaypointIndex()
        {
            int count = _sortedWaypoints?.Count ?? 0;
            if (count == 0 || player == null)
                return 0;

            int closest = 0;
            float minDist = float.MaxValue;
            Vector3 playerPos = player.position;

            for (int i = 0; i < count; i++)
            {
                Transform wp = _sortedWaypoints[i];
                if (wp == null)
                    continue;

                float dist = Vector3.Distance(playerPos, wp.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = i;
                }
            }

            return closest;
        }

        /// <summary>Entry point for Next Point input; respects enableWaypoints and logs when disabled.</summary>
        private void TryTeleportToNextPoint()
        {
            if (!enableWaypoints)
            {
                if (showDebugLogs)
                    Debug.Log("[CheatManager] Waypoints Disabled.");
                return;
            }
            TeleportToNextPoint();
        }

        /// <summary>Entry point for Previous Point input; respects enableWaypoints and logs when disabled.</summary>
        private void TryTeleportToPreviousPoint()
        {
            if (!enableWaypoints)
            {
                if (showDebugLogs)
                    Debug.Log("[CheatManager] Waypoints Disabled.");
                return;
            }
            TeleportToPreviousPoint();
        }

        /// <summary>
        /// Snap &amp; step: find closest waypoint, then target closestIndex + 1 (wrap to 0).
        /// Forces movement to a different waypoint even when player is standing on the current one.
        /// </summary>
        public void TeleportToNextPoint()
        {
            if (!TryGetValidWaypointState(out int count))
                return;

            int closestIndex = GetClosestWaypointIndex();
            int targetIndex = closestIndex + 1;

            if (targetIndex >= count)
                targetIndex = 0;

            TeleportToWaypoint(targetIndex);
        }

        /// <summary>
        /// Snap &amp; step: find closest waypoint, then target closestIndex - 1 (wrap to last).
        /// Forces movement to a different waypoint even when player is standing on the current one.
        /// </summary>
        public void TeleportToPreviousPoint()
        {
            if (!TryGetValidWaypointState(out int count))
                return;

            int closestIndex = GetClosestWaypointIndex();
            int targetIndex = closestIndex - 1;

            if (targetIndex < 0)
                targetIndex = count - 1;

            TeleportToWaypoint(targetIndex);
        }

        /// <summary>Returns false if waypoints list is empty or player is null.</summary>
        private bool TryGetValidWaypointState(out int waypointCount)
        {
            waypointCount = _sortedWaypoints?.Count ?? 0;
            if (waypointCount == 0 || player == null)
                return false;
            return true;
        }

        /// <summary>Moves the player to the waypoint at index and updates _currentWaypointIndex.</summary>
        private void TeleportToWaypoint(int index)
        {
            if (_sortedWaypoints == null || index < 0 || index >= _sortedWaypoints.Count || player == null)
                return;

            Transform target = _sortedWaypoints[index];
            if (target == null)
                return;

            Vector3 pos = target.position;
            player.position = new Vector3(pos.x, pos.y, player.position.z);

            _currentWaypointIndex = index;

            if (showDebugLogs)
                Debug.Log($"[CheatManager] Teleporting to Waypoint {index} ({target.name}).");
        }

#if UNITY_EDITOR
        /// <summary>Call from Editor or runtime to re-sort waypoints after changing the list in Inspector.</summary>
        public void EditorSortWaypoints()
        {
            SortAndCacheWaypoints();
        }
#endif
    }
}

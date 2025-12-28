using System.Collections.Generic;
using UnityEngine;

public class NpcPath : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Ordered list: element 0 is Start, last element is End")]
    [SerializeField] private List<NpcWaypoint> waypoints = new();

    public IReadOnlyList<NpcWaypoint> Waypoints => waypoints;

    public NpcWaypoint GetStart()
    {
        return waypoints != null && waypoints.Count > 0 ? waypoints[0] : null;
    }

    public NpcWaypoint GetEnd()
    {
        return waypoints != null && waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : null;
    }

    private void OnValidate()
    {
        waypoints ??= new List<NpcWaypoint>();
    }
}

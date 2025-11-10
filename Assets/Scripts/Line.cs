// Clean Line.cs that mirrors the user's provided implementation (points + circle colliders + edge collider + physics toggle)
using UnityEngine;
using System.Collections.Generic;

public class Line : MonoBehaviour
{
    public LineRenderer lineRenderer;
    [Tooltip("Used only while drawing; disabled after FinalizeLine when polygonCollider is created.")]
    public EdgeCollider2D edgeCollider;
    [Tooltip("Assigned/created at FinalizeLine; used for mass/physics after drawing.")]
    public PolygonCollider2D polygonCollider;
    public Rigidbody2D rigidBody;
    [Tooltip("Add a CircleCollider2D per point (heavier). Leave OFF for better performance.")]
    public bool addCircleColliderPerPoint = false;
    [Tooltip("Use a PolygonCollider2D with thickness when finalizing the line so it has mass and can fall.")]
    public bool usePolygonCollider = true;

    // Stored ALWAYS in LOCAL space (relative to this transform).
    [HideInInspector] public List<Vector2> points = new List<Vector2>();
    [HideInInspector] public int pointsCount = 0;

    float pointsMinDistance = 0.1f; // configurable via setter
    float circleColliderRadius;      // updated when SetLineWidth is called

    private PhysicsMaterial2D _physicsMat;

    /// <summary>
    /// Initialize visual/physics parameters when a new line is spawned.
    /// </summary>
    public void Initialize(Material mat, float width, float minDist, PhysicsMaterial2D physicsMat, bool usePolygon)
    {
        // Ensure required components
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if (!edgeCollider) edgeCollider = GetComponent<EdgeCollider2D>();
        if (!rigidBody) rigidBody = GetComponent<Rigidbody2D>();

        // We will work in LOCAL space so colliders and renderer match exactly.
        lineRenderer.useWorldSpace = false;

        // Apply material
        if (mat != null) lineRenderer.material = mat;

        // Apply configurable values
        SetLineWidth(width);
        SetPointsMinDistance(minDist);

        // Physics material for edge collider if provided
        _physicsMat = physicsMat;
        if (_physicsMat != null) edgeCollider.sharedMaterial = _physicsMat;

        // Start without physics while drawing
        UsePhysics(false);

        usePolygonCollider = usePolygon;
    }

    /// <summary>
    /// Add a LOCAL point (already converted relative to this transform).
    /// </summary>
    public void AddPoint(Vector2 newPoint)
    {
        if (pointsCount >= 1 && Vector2.Distance(newPoint, GetLastPoint()) < pointsMinDistance)
            return;

        points.Add(newPoint);
        pointsCount++;

        // Optional: add small circle collider at this point (offset already local)
        if (addCircleColliderPerPoint)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.offset = newPoint;
            circle.radius = circleColliderRadius;
        }

    // LineRenderer update (LOCAL positions)
    if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
    lineRenderer.positionCount = pointsCount;
    lineRenderer.SetPosition(pointsCount - 1, new Vector3(newPoint.x, newPoint.y, 0f));

        // EdgeCollider needs at least 2 points
        if (!edgeCollider) edgeCollider = GetComponent<EdgeCollider2D>();
        if (pointsCount > 1) edgeCollider.points = points.ToArray();
    }

    /// <summary>
    /// Add a WORLD point (converts internally to local). Use this from manager.
    /// On the FIRST point we move the whole line object to that world position so local = 0.
    /// </summary>
    public void AddWorldPoint(Vector2 worldPoint)
    {
        if (pointsCount == 0)
        {
            // Place transform at first world point origin
            transform.position = worldPoint;
            AddPoint(Vector2.zero);
        }
        else
        {
            Vector2 local = transform.InverseTransformPoint(worldPoint);
            AddPoint(local);
        }
    }

    public Vector2 GetLastPoint()
    {
        if (pointsCount == 0) return Vector2.zero;
        return points[pointsCount - 1];
    }

    public void UsePhysics(bool usePhysics)
    {
        if (!rigidBody) rigidBody = GetComponent<Rigidbody2D>();
        //rigidBody.isKinematic = !usePhysics; // false => physics active
        rigidBody.bodyType = usePhysics ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }

    public void SetLineColor(Gradient colorGradient)
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.colorGradient = colorGradient;
    }

    public void SetPointsMinDistance(float distance)
    {
        pointsMinDistance = Mathf.Max(0.0001f, distance);
    }

    public void SetLineWidth(float width)
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        circleColliderRadius = width * 0.5f;
        if (!edgeCollider) edgeCollider = GetComponent<EdgeCollider2D>();
        edgeCollider.edgeRadius = circleColliderRadius * 0.9f; // tube-like thickness without per-point circles
    }

    /// <summary>
    /// Build a solid polygon from the drawn points and switch to a PolygonCollider2D so the line has mass and can fall.
    /// </summary>
    public void FinalizeLine(bool makeDynamic)
    {
        // Ensure width is set (safety if Initialize skipped)
        if (lineRenderer && lineRenderer.positionCount == 0 && pointsCount > 0)
        {
            lineRenderer.positionCount = pointsCount;
            for (int i = 0; i < pointsCount; i++)
                lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
        }

        if (usePolygonCollider && points.Count >= 2)
        {
            if (!polygonCollider) polygonCollider = GetComponent<PolygonCollider2D>();
            if (!polygonCollider) polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            if (_physicsMat != null) polygonCollider.sharedMaterial = _physicsMat;

            // Build thick path with current line width
            float width = lineRenderer ? lineRenderer.startWidth : (circleColliderRadius * 2f);
            Vector2[] path = BuildThickPath(points, width);
            if (path != null && path.Length >= 3)
            {
                polygonCollider.pathCount = 1;
                polygonCollider.SetPath(0, path);
            }

            // Disable edge collider; polygon now handles collisions
            if (edgeCollider) edgeCollider.enabled = false;
        }
        else if (!usePolygonCollider && edgeCollider)
        {
            // Ensure edgeCollider has final points (already set during drawing)
            if (pointsCount > 1) edgeCollider.points = points.ToArray();
        }

        UsePhysics(makeDynamic);
    }

    // Create a polygon “ribbon” around the polyline in local space
    private Vector2[] BuildThickPath(List<Vector2> localPts, float width)
    {
        if (localPts == null || localPts.Count < 2) return null;

        float half = Mathf.Max(0.001f, width * 0.5f);

        var left = new List<Vector2>(localPts.Count);
        var right = new List<Vector2>(localPts.Count);
        for (int i = 0; i < localPts.Count; i++)
        {
            Vector2 forward;
            if (i == 0) forward = (localPts[1] - localPts[0]).normalized;
            else if (i == localPts.Count - 1) forward = (localPts[i] - localPts[i - 1]).normalized;
            else
            {
                Vector2 d1 = (localPts[i] - localPts[i - 1]).normalized;
                Vector2 d2 = (localPts[i + 1] - localPts[i]).normalized;
                forward = (d1 + d2).normalized;
                if (forward == Vector2.zero) forward = d1;
            }
            Vector2 normal = new Vector2(-forward.y, forward.x);
            Vector2 p = localPts[i];
            left.Add(p + normal * half);
            right.Add(p - normal * half);
        }

        var poly = new List<Vector2>(left);
        for (int i = right.Count - 1; i >= 0; i--) poly.Add(right[i]);
        return poly.ToArray();
    }
}
// Clean Line.cs that mirrors the user's provided implementation (points + circle colliders + edge collider + physics toggle)

using System.Collections.Generic;
using UnityEngine;

namespace Drawing.LineControl
{
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
        [Tooltip("Simplification tolerance (world units) used before baking colliders. Higher = fewer points.")]
        public float colliderSimplifyTolerance = 0.03f;
        [Tooltip("Clamp the number of points used to build the collider. 0 = unlimited (not recommended).")]
        public int maxColliderPoints = 128;
        [Tooltip("Destroy the not-used collider component after finalize to reduce overhead on many line objects.")]
        public bool destroyUnusedCollidersOnFinalize = true;

        // Stored ALWAYS in LOCAL space (relative to this transform).
        [HideInInspector] public List<Vector2> points = new List<Vector2>();
        [HideInInspector] public int pointsCount = 0;

        float pointsMinDistance = 0.1f; // configurable via setter
        float circleColliderRadius;      // updated when SetLineWidth is called

        private PhysicsMaterial2D _physicsMat;

        /// <summary>
        /// Initialize visual/physics parameters when a new line is spawned.
        /// Material is expected to be set on the prefab/LineRenderer; we don't reassign it here.
        /// </summary>
        public void Initialize(float width, float minDist, PhysicsMaterial2D physicsMat, bool usePolygon, bool collideWhileDrawing = false, float simplifyTolerance = -1f, int maxPoints = -1,Gradient colorGradient = null)
        {
            // Ensure required components
            if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
            if (!rigidBody) rigidBody = GetComponent<Rigidbody2D>();
            if (colorGradient != null)
            {
                lineRenderer.colorGradient = colorGradient;
            }
            // We will work in LOCAL space so colliders and renderer match exactly.
            lineRenderer.useWorldSpace = false;

            // Apply configurable values
            SetLineWidth(width);
            SetPointsMinDistance(minDist);

            if (simplifyTolerance >= 0f) colliderSimplifyTolerance = simplifyTolerance;
            if (maxPoints >= 0) maxColliderPoints = maxPoints;

            // Physics material for edge collider if provided (only if we are colliding while drawing)
            _physicsMat = physicsMat;

            if (collideWhileDrawing)
            {
                if (!edgeCollider) edgeCollider = GetComponent<EdgeCollider2D>();
                if (_physicsMat != null && edgeCollider != null) edgeCollider.sharedMaterial = physicsMat;
                if (edgeCollider != null) edgeCollider.enabled = true;
                
            }
            else
            {
                // Don't spend time maintaining an edge collider while drawing
                if (edgeCollider) edgeCollider.enabled = false;
            }

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
            lineRenderer.positionCount = pointsCount;
            lineRenderer.SetPosition(pointsCount - 1, new Vector3(newPoint.x, newPoint.y, 0f));

            // EdgeCollider updates are expensive (allocations). Only do it if enabled and we have enough points.
            if (edgeCollider && edgeCollider.enabled && pointsCount > 1)
            {
                edgeCollider.points = points.ToArray();
            }
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
            if (edgeCollider) edgeCollider.edgeRadius = circleColliderRadius * 0.9f; // tube-like thickness without per-point circles
        }

        /// <summary>
        /// Build a solid polygon from the drawn points and switch to a PolygonCollider2D so the line has mass and can fall.
        /// </summary>
        public void FinalizeLine(LineSettings lineSetting = null)
        {
            var config = lineSetting;
            if(config == null) config= DrawingConfigController.Instance.GetCurrentSettings();
            // Ensure width is set (safety if Initialize skipped)
            if (lineRenderer && lineRenderer.positionCount == 0 && pointsCount > 0)
            {
                lineRenderer.positionCount = pointsCount;
                for (int i = 0; i < pointsCount; i++)
                    lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
            }

            // Prepare a simplified copy of points for physics (work in LOCAL space)
            List<Vector2> physicsPts = points;
            if (colliderSimplifyTolerance > 0f && points.Count > 2)
            {
                physicsPts = new List<Vector2>(points);
                physicsPts = SimplifyRdp(physicsPts, colliderSimplifyTolerance);
            }
            if (maxColliderPoints > 0 && physicsPts.Count > maxColliderPoints)
            {
                physicsPts = ResampleByCount(physicsPts, maxColliderPoints);
            }

            if (usePolygonCollider && physicsPts.Count >= 2)
            {
                if (!polygonCollider) polygonCollider = GetComponent<PolygonCollider2D>();
                if (!polygonCollider) polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
                if (_physicsMat != null) polygonCollider.sharedMaterial = _physicsMat;

                // Build thick path with current line width
                float width = lineRenderer ? lineRenderer.startWidth : (circleColliderRadius * 2f);
                Vector2[] path = BuildThickPath(physicsPts, width);
                if (path != null && path.Length >= 3)
                {
                    polygonCollider.pathCount = 1;
                    polygonCollider.SetPath(0, path);
                }

                // Disable edge collider; polygon now handles collisions
                if (edgeCollider)
                {
                    if (destroyUnusedCollidersOnFinalize) Destroy(edgeCollider);
                    else edgeCollider.enabled = false;
                }
            }
            else if (!usePolygonCollider)
            {
                // Ensure we have an edge collider and set it once now
                if (!edgeCollider) edgeCollider = GetComponent<EdgeCollider2D>();
                if (!edgeCollider) edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
                if (_physicsMat != null) edgeCollider.sharedMaterial = _physicsMat;
                edgeCollider.enabled = true;
                if (physicsPts.Count > 1) edgeCollider.points = physicsPts.ToArray();
                // If polygon collider exists but not used, optionally remove
                if (polygonCollider && destroyUnusedCollidersOnFinalize) Destroy(polygonCollider);
            }

            // Light-weight physics defaults
            if (!rigidBody) rigidBody = GetComponent<Rigidbody2D>();
            if (rigidBody)
            {
                rigidBody.useAutoMass = false;
                rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rigidBody.sleepMode = RigidbodySleepMode2D.StartAsleep;
                // Approximate mass from length * width
                float width = lineRenderer ? lineRenderer.startWidth : (circleColliderRadius * 2f);
                float length = 0f;
                for (int i = 1; i < physicsPts.Count; i++) length += Vector2.Distance(physicsPts[i - 1], physicsPts[i]);
                float area = Mathf.Max(0.0001f, length * width);
                rigidBody.mass = Mathf.Clamp(area, 0.1f, 5f);
                SetGravity(config);
            }

            UsePhysics(config.usePhysics);
        }

        private void SetGravity(LineSettings config)
        {
            if (config.changeGravityScale)
            {
                rigidBody.gravityScale = config.gravityScaleOverride;
            }
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

        // Ramer–Douglas–Peucker simplification in LOCAL space
        private static List<Vector2> SimplifyRdp(List<Vector2> pts, float tolerance)
        {
            if (pts == null || pts.Count < 3) return pts;

            var keep = new bool[pts.Count];
            keep[0] = keep[pts.Count - 1] = true;

            var stack = new Stack<(int, int)>();
            stack.Push((0, pts.Count - 1));

            float sqTol = tolerance * tolerance;
            while (stack.Count > 0)
            {
                var (start, end) = stack.Pop();
                float maxDist = 0f; int index = -1;
                Vector2 a = pts[start];
                Vector2 b = pts[end];
                Vector2 ab = b - a;
                float abLenSq = ab.sqrMagnitude + 1e-12f;
                for (int i = start + 1; i < end; i++)
                {
                    Vector2 ap = pts[i] - a;
                    float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abLenSq);
                    Vector2 proj = a + ab * t;
                    float dSq = (pts[i] - proj).sqrMagnitude;
                    if (dSq > maxDist)
                    {
                        maxDist = dSq; index = i;
                    }
                }
                if (maxDist > sqTol && index != -1)
                {
                    keep[index] = true;
                    stack.Push((start, index));
                    stack.Push((index, end));
                }
            }

            var outPts = new List<Vector2>();
            for (int i = 0; i < pts.Count; i++) if (keep[i]) outPts.Add(pts[i]);
            return outPts;
        }

        // Uniformly resample a polyline to a fixed max count (including endpoints)
        private static List<Vector2> ResampleByCount(List<Vector2> pts, int maxCount)
        {
            if (pts.Count <= maxCount) return pts;
            float total = 0f; for (int i = 1; i < pts.Count; i++) total += Vector2.Distance(pts[i - 1], pts[i]);
            if (total <= 1e-6f) return new List<Vector2> { pts[0], pts[pts.Count - 1] };
            int target = Mathf.Max(2, maxCount);
            float step = total / (target - 1);
            var result = new List<Vector2>(target);
            result.Add(pts[0]);
            float acc = 0f; int seg = 1; float distToNext = step;
            while (result.Count < target - 1)
            {
                if (seg >= pts.Count) break;
                Vector2 a = pts[seg - 1];
                Vector2 b = pts[seg];
                float segLen = Vector2.Distance(a, b);
                if (segLen >= distToNext)
                {
                    float t = distToNext / segLen;
                    Vector2 p = Vector2.Lerp(a, b, t);
                    result.Add(p);
                    pts[seg - 1] = p; // advance within segment
                    distToNext = step;
                }
                else
                {
                    distToNext -= segLen;
                    seg++;
                }
            }
            result.Add(pts[pts.Count - 1]);
            return result;
        }
    }
}
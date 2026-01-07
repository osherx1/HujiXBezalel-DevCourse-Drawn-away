using System.Collections.Generic;
using Drawing.LineControl;
using UnityEngine;

namespace Utilities.Editor
{
    /// <summary>
    /// Static utility class for analyzing Line objects' geometry and physics properties.
    /// Centralizes metric calculations to eliminate code duplication (DRY principle).
    /// </summary>
    public static class LineAnalyzerUtils
    {
        /// <summary>
        /// Extracts world space points from a Line object for persistent visualization.
        /// Uses only LineRenderer (line.points is private).
        /// </summary>
        public static Vector3[] ExtractWorldPoints(Line line)
        {
            if (line == null)
                return new Vector3[0];

            try
            {
                if (line.gameObject == null || line.lineRenderer == null)
                    return new Vector3[0];

                List<Vector3> worldPointsList = new List<Vector3>();

                // Get points from LineRenderer (uses local space, so convert to world)
                if (line.lineRenderer.positionCount > 0)
                {
                    for (int i = 0; i < line.lineRenderer.positionCount; i++)
                    {
                        try
                        {
                            Vector3 localPos = line.lineRenderer.GetPosition(i);
                            Vector3 worldPos = line.transform.TransformPoint(localPos);
                            worldPointsList.Add(worldPos);
                        }
                        catch
                        {
                            // Skip invalid points
                            continue;
                        }
                    }
                }

                return worldPointsList.ToArray();
            }
            catch
            {
                // Gracefully handle any errors (object destroyed, missing components, etc.)
                return new Vector3[0];
            }
        }

        /// <summary>
        /// Calculates geometry metrics: length, point count, and straightness ratio.
        /// Uses only LineRenderer (line.points is private).
        /// </summary>
        public static void CalculateGeometryMetrics(Line line, out float length, out int pointCount, out float straightness)
        {
            length = 0f;
            pointCount = 0;
            straightness = 1f;

            if (line == null)
                return;

            try
            {
                if (line.lineRenderer == null)
                    return;

                // Use LineLength property if available (already calculated)
                try
                {
                    if (line.LineLength > 0f)
                    {
                        length = line.LineLength;
                    }
                    else if (line.lineRenderer.positionCount >= 2)
                    {
                        // Calculate from LineRenderer positions
                        for (int i = 1; i < line.lineRenderer.positionCount; i++)
                        {
                            try
                            {
                                Vector3 p1 = line.lineRenderer.GetPosition(i - 1);
                                Vector3 p2 = line.lineRenderer.GetPosition(i);
                                length += Vector3.Distance(p1, p2);
                            }
                            catch
                            {
                                // Skip invalid segment
                                continue;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback: calculate manually if LineLength property fails
                    if (line.lineRenderer.positionCount >= 2)
                    {
                        for (int i = 1; i < line.lineRenderer.positionCount; i++)
                        {
                            try
                            {
                                Vector3 p1 = line.lineRenderer.GetPosition(i - 1);
                                Vector3 p2 = line.lineRenderer.GetPosition(i);
                                length += Vector3.Distance(p1, p2);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }

                // Get point count from LineRenderer
                try
                {
                    pointCount = line.lineRenderer.positionCount;
                }
                catch
                {
                    pointCount = 0;
                }

                // Calculate straightness: Use GetStraightnessRatio if available, otherwise calculate manually
                if (line.lineRenderer.positionCount >= 2 && length > 0.0001f)
                {
                    try
                    {
                        // Try to use the method if it exists
                        straightness = line.GetStraightnessRatio();
                    }
                    catch
                    {
                        // Fallback: Manual calculation from LineRenderer
                        try
                        {
                            Vector3 startPoint = line.lineRenderer.GetPosition(0);
                            Vector3 endPoint = line.lineRenderer.GetPosition(line.lineRenderer.positionCount - 1);
                            float directDistance = Vector3.Distance(startPoint, endPoint);
                            straightness = directDistance / length;
                        }
                        catch
                        {
                            straightness = 1f; // Default for invalid calculation
                        }
                    }
                }
                else
                {
                    straightness = 1f; // Default for invalid lines
                }
            }
            catch
            {
                // Gracefully handle any errors
                length = 0f;
                pointCount = 0;
                straightness = 1f;
            }
        }

        /// <summary>
        /// Calculates physics metrics: center of mass, velocity, angular velocity, and bounds area.
        /// </summary>
        public static void CalculatePhysicsMetrics(Line line, out Vector3 centerOfMass, out float velocityMagnitude,
            out float angularVelocity, out float boundsArea)
        {
            centerOfMass = Vector3.zero;
            velocityMagnitude = 0f;
            angularVelocity = 0f;
            boundsArea = 0f;

            if (line == null)
                return;

            try
            {
                if (line.gameObject == null)
                    return;

                // Center of Mass (from Rigidbody2D or Collider bounds)
                try
                {
                    if (line.rigidBody != null)
                    {
                        // Rigidbody2D center of mass is in local space, convert to world
                        centerOfMass = line.transform.TransformPoint(line.rigidBody.centerOfMass);

                        // Velocity (handle sleeping/kinematic bodies)
                        try
                        {
                            if (line.rigidBody.bodyType != RigidbodyType2D.Kinematic && !line.rigidBody.IsSleeping())
                            {
                                velocityMagnitude = line.rigidBody.linearVelocity.magnitude;
                                angularVelocity = line.rigidBody.angularVelocity;
                            }
                            else
                            {
                                velocityMagnitude = 0f;
                                angularVelocity = 0f;
                            }
                        }
                        catch
                        {
                            // Rigidbody might be destroyed or invalid
                            velocityMagnitude = 0f;
                            angularVelocity = 0f;
                        }
                    }
                    else
                    {
                        // Fallback: Use transform position
                        centerOfMass = line.transform.position;
                    }
                }
                catch
                {
                    // Fallback: Use transform position if rigidbody access fails
                    try
                    {
                        centerOfMass = line.transform.position;
                    }
                    catch
                    {
                        centerOfMass = Vector3.zero;
                    }
                }

                // Bounds Area (from Collider2D)
                try
                {
                    Collider2D collider = line.polygonCollider != null ? (Collider2D)line.polygonCollider : line.edgeCollider;
                    if (collider != null && collider.enabled)
                    {
                        Bounds bounds = collider.bounds;
                        boundsArea = bounds.size.x * bounds.size.y;
                    }
                    else if (line.lineRenderer != null)
                    {
                        // Fallback: Estimate from LineRenderer bounds
                        try
                        {
                            Bounds rendererBounds = line.lineRenderer.bounds;
                            boundsArea = rendererBounds.size.x * rendererBounds.size.y;
                        }
                        catch
                        {
                            boundsArea = 0f;
                        }
                    }
                }
                catch
                {
                    // Fallback: Try LineRenderer if collider access fails
                    try
                    {
                        if (line.lineRenderer != null)
                        {
                            Bounds rendererBounds = line.lineRenderer.bounds;
                            boundsArea = rendererBounds.size.x * rendererBounds.size.y;
                        }
                    }
                    catch
                    {
                        boundsArea = 0f;
                    }
                }
            }
            catch
            {
                // Gracefully handle any errors
                centerOfMass = Vector3.zero;
                velocityMagnitude = 0f;
                angularVelocity = 0f;
                boundsArea = 0f;
            }
        }
    }
}


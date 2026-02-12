using System.Collections.Generic;
using UnityEngine;

namespace Drawing.VFX
{
    /// <summary>
    /// Static utility class for line-related mathematical calculations.
    /// Keeps logic independent of Unity components for testability and reuse.
    /// </summary>
    public static class LineMathUtils
    {
        /// <summary>
        /// Calculates equidistant points along a polyline and writes them into the output buffer.
        /// Supports either a fixed number of points or spacing-based distribution.
        /// </summary>
        /// <param name="worldPoints">Polyline vertices in world space (at least 2 points).</param>
        /// <param name="useSpacing">If true, point count is derived from total length / spacing; otherwise uses fixedCount.</param>
        /// <param name="fixedCount">Used when useSpacing is false: desired number of output points.</param>
        /// <param name="spacingDist">Used when useSpacing is true: minimum distance between consecutive output points.</param>
        /// <param name="outputBuffer">List to fill with equidistant points; will be cleared before use.</param>
        public static void CalculateEquidistantPoints(
            IList<Vector2> worldPoints,
            bool useSpacing,
            int fixedCount,
            float spacingDist,
            List<Vector2> outputBuffer)
        {
            outputBuffer?.Clear();
            if (worldPoints == null || worldPoints.Count < 2 || outputBuffer == null)
                return;

            // 1. Compute total length
            float totalLength = 0f;
            for (int i = 1; i < worldPoints.Count; i++)
                totalLength += Vector2.Distance(worldPoints[i - 1], worldPoints[i]);

            if (totalLength <= Mathf.Epsilon)
                return;

            // 2. Determine target point count
            int finalPointCount = fixedCount;
            if (useSpacing && spacingDist > 0f)
                finalPointCount = Mathf.FloorToInt(totalLength / spacingDist) + 1;
            finalPointCount = Mathf.Max(2, finalPointCount);

            // 3. Walk the path and sample equidistant points
            float stepDistance = totalLength / (finalPointCount - 1);
            float currentTraveled = 0f;
            int currentSegmentIndex = 0;

            outputBuffer.Add(worldPoints[0]);

            for (int i = 1; i < finalPointCount - 1; i++)
            {
                float targetDist = i * stepDistance;

                while (currentSegmentIndex < worldPoints.Count - 1)
                {
                    float segmentLength = Vector2.Distance(worldPoints[currentSegmentIndex], worldPoints[currentSegmentIndex + 1]);

                    if (currentTraveled + segmentLength >= targetDist)
                    {
                        float distanceIntoSegment = targetDist - currentTraveled;
                        float t = segmentLength > Mathf.Epsilon ? distanceIntoSegment / segmentLength : 0f;
                        Vector2 newPoint = Vector2.Lerp(worldPoints[currentSegmentIndex], worldPoints[currentSegmentIndex + 1], t);
                        outputBuffer.Add(newPoint);
                        break;
                    }

                    currentTraveled += segmentLength;
                    currentSegmentIndex++;
                }
            }

            outputBuffer.Add(worldPoints[worldPoints.Count - 1]);
        }
    }
}

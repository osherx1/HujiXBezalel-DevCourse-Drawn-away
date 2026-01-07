using System;
using System.Collections.Generic;
using Drawing.LineControl;
using UnityEngine;

namespace Utilities.Editor
{
    /// <summary>
    /// Serializable snapshot of a Line's data (survives GameObject destruction).
    /// Used for persistent analysis after Play Mode ends.
    /// </summary>
    [Serializable]
    public class LineSnapshot
    {
        // Basic Properties
        public string settingID;
        public float mass;
        public float inkCost;
        public float creationTime; // Time relative to session start (in seconds)
        public bool isOutlier;
        public int lineIndex; // Sequential index for display
        public string gameObjectName; // For reference (may be null after destruction)
        public Vector3[] worldPoints; // World space positions for Scene View visualization

        // Geometry Metrics
        public float length; // Total length of the line (sum of distances between points)
        public int pointCount; // Number of vertices
        public float straightness; // Ratio: Distance(Start, End) / TotalLength (1.0 = straight, low = scribbly)

        // Physics Metrics
        public Vector3 centerOfMass; // World position of the center of mass
        public float velocityMagnitude; // Speed at snapshot time
        public float angularVelocity; // Rotation speed (rad/s)
        public float boundsArea; // Approximate size in world (Collider2D.bounds.size.x * y)

        public LineSnapshot(Line line, float creationTimeRelative, int index)
        {
            if (line != null)
            {
                try
                {
                    settingID = string.IsNullOrEmpty(line.SettingID) ? "Unknown" : line.SettingID;
                    mass = line.rigidBody != null ? line.rigidBody.mass : 0f;
                    inkCost = line.InkCost;
                    gameObjectName = line.gameObject != null ? line.gameObject.name : "Unknown";

                    // Extract world space points from Line for Scene View visualization
                    worldPoints = LineAnalyzerUtils.ExtractWorldPoints(line);

                    // Calculate Geometry Metrics using centralized utility
                    LineAnalyzerUtils.CalculateGeometryMetrics(line, out length, out pointCount, out straightness);

                    // Calculate Physics Metrics using centralized utility
                    LineAnalyzerUtils.CalculatePhysicsMetrics(line, out centerOfMass, out velocityMagnitude, out angularVelocity, out boundsArea);
                }
                catch
                {
                    // Gracefully handle errors if line is destroyed mid-operation
                    settingID = "Unknown";
                    mass = 0f;
                    inkCost = 0f;
                    gameObjectName = "Unknown";
                    worldPoints = new Vector3[0];
                    length = 0f;
                    pointCount = 0;
                    straightness = 1f;
                    centerOfMass = Vector3.zero;
                    velocityMagnitude = 0f;
                    angularVelocity = 0f;
                    boundsArea = 0f;
                }
            }
            else
            {
                settingID = "Unknown";
                mass = 0f;
                inkCost = 0f;
                gameObjectName = "Unknown";
                worldPoints = new Vector3[0];
                length = 0f;
                pointCount = 0;
                straightness = 1f;
                centerOfMass = Vector3.zero;
                velocityMagnitude = 0f;
                angularVelocity = 0f;
                boundsArea = 0f;
            }

            this.creationTime = creationTimeRelative;
            isOutlier = false; // Will be set later
            lineIndex = index;
        }

        // Constructor for creating from data (when Line is null)
        public LineSnapshot(string settingID, float mass, float inkCost, float creationTime, int index, string gameObjectName = "Unknown")
        {
            this.settingID = settingID;
            this.mass = mass;
            this.inkCost = inkCost;
            this.creationTime = creationTime;
            this.lineIndex = index;
            this.gameObjectName = gameObjectName;
            this.isOutlier = false;
            this.worldPoints = new Vector3[0]; // No geometry data available

            // Default values when Line is not available
            this.length = 0f;
            this.pointCount = 0;
            this.straightness = 1f;
            this.centerOfMass = Vector3.zero;
            this.velocityMagnitude = 0f;
            this.angularVelocity = 0f;
            this.boundsArea = 0f;
        }
    }

    /// <summary>
    /// Serializable container for persistent session data.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public List<LineSnapshot> lineSnapshots = new List<LineSnapshot>();
        public float sessionStartTime = -1f;
        public float sessionEndTime = -1f;
        public bool gameFinished;
    }

    /// <summary>
    /// Pre-calculated chart visualization data for performance optimization.
    /// </summary>
    public class ChartData
    {
        public List<float> normalizedHeights = new List<float>();
        public List<Color> barColors = new List<Color>();
        public List<string> tooltipTexts = new List<string>();
        public List<Line> chartLines = new List<Line>(); // Lines corresponding to each bar
        public float maxMass;
        public bool isValid = false;
    }

    /// <summary>
    /// Container for statistics grouped by settingID.
    /// </summary>
    public class SettingIDGroup
    {
        public string settingID;
        public int count;
        public float averageMass;
        public float minMass;
        public float maxMass;
        public float totalInkCost;
        public List<Line> lines = new List<Line>(); // Runtime references (may be null after Play Mode)
        public List<LineSnapshot> snapshots = new List<LineSnapshot>(); // Persistent snapshots
        public List<Line> outlierLines = new List<Line>();
        public List<LineSnapshot> outlierSnapshots = new List<LineSnapshot>();
        public bool foldoutExpanded = true; // Default to expanded

        // Advanced Metrics Averages
        public float averageLength;
        public float averageStraightness;
        public float averageVelocity;

        // Outlier Detection Statistics (Z-Score based)
        public float standardDeviation;
        public float variance;

        // Pre-calculated chart data for performance
        public ChartData chartData = new ChartData();
    }

    /// <summary>
    /// Container for outlier information (supports both runtime and snapshot data).
    /// </summary>
    public class OutlierInfo
    {
        public Line line; // May be null after Play Mode
        public LineSnapshot snapshot; // Always available
        public string settingID;
        public float mass;
        public float averageMass;
        public float deviationFactor;
        public float inkCost;
        public float creationTime;
    }

    /// <summary>
    /// Container for individual line data (used for CSV export and detailed view).
    /// </summary>
    public class LineData
    {
        public string settingID;
        public float mass;
        public float inkCost;
        public bool isOutlier;
        public float creationTime;

        // Geometry Metrics
        public float length;
        public int pointCount;
        public float straightness;

        // Physics Metrics
        public Vector3 centerOfMass;
        public float velocityMagnitude;
        public float angularVelocity;
        public float boundsArea;
    }
}


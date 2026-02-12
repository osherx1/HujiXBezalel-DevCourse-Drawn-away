using System.Collections.Generic;
using UnityEngine;
using Drawing.LineControl; // Assuming this namespace exists based on input

namespace Drawing.VFX
{
    [RequireComponent(typeof(LineManager))]
    public class LineVFXHandler : MonoBehaviour
    {
        #region Settings - Distribution
        [Header("Points Distribution Settings")]
        [Tooltip("If true, calculates points based on distance. If false, uses fixed number.")]
        [SerializeField] private bool _useSpacingMode = false;

        [Tooltip("Used if UseSpacingMode is FALSE: Fixed number of burst points.")]
        [Min(2)] 
        [SerializeField] private int _pointsNumber = 5;

        [Tooltip("Used if UseSpacingMode is TRUE: Minimum distance between burst points.")]
        [Min(0.01f)]
        [SerializeField] private float _minSpacing = 0.5f;
        #endregion

        #region Settings - Particles
        [Header("Particle Systems")]
        [Tooltip("The parent GameObject containing all drawing VFX (Trails, Sparkles, etc.)")]
        [SerializeField] private ParticleSystem _trailHierarchyRoot;
        
        [Tooltip("The main 'brush' effect that follows the tip.")]
        [SerializeField] private ParticleSystem _brushTipParticles;

        [Tooltip("Burst effect triggered when the line is finalized.")]
        [SerializeField] private ParticleSystem _finalizeBurstParticles;

        [Header("Burst Visuals")]
        [SerializeField] private int _particlesPerBurst = 15;
        [SerializeField] private Color _burstColor = Color.white;
        #endregion

        // Dependencies & State
       //private LineManager _lineManager;
        
        // Caching lists to avoid Garbage Collection (GC) allocations per line
        private readonly List<Vector2> _cachedWorldPoints = new List<Vector2>(100);
        private readonly List<Vector2> _equidistantPointsBuffer = new List<Vector2>(20);
        public bool IsPlaying { get; private set; }


        public void HandleLineStarted(Line line)
        {
            if (line == null|| IsPlaying) return;
            IsPlaying = true;
            // Reset and activate drawing effects
            UpdateTrailPosition(line.transform.position);
            ToggleTrailEffects(true);
            ActivateBrushTip(line.transform.position);
        }

        public void HandleLineFinished(Line line)
        {
            if (line == null || !IsPlaying) return;

            // 1. Build world-space points and calculate equidistant points for the burst
            BuildWorldPointsFromLine(line, _cachedWorldPoints);
            LineMathUtils.CalculateEquidistantPoints(_cachedWorldPoints, _useSpacingMode, _pointsNumber, _minSpacing, _equidistantPointsBuffer);

            // 2. Spawn the burst
            SpawnBurstParticles(_equidistantPointsBuffer);

            // 3. Cleanup
            DeactivateBrushTip();
            ToggleTrailEffects(false);
            IsPlaying = false;
        }

        // Called presumably by LineManager during Update (if not, should be added there)
        public void UpdateTrailPosition(Vector2 pos)
        {
            if (_trailHierarchyRoot != null)
                _trailHierarchyRoot.transform.position = pos;

            if (_brushTipParticles != null)
                _brushTipParticles.transform.position = pos;
        }

    

        #region Particle Control

        /// <summary>
        /// Controls the entire trail hierarchy.
        /// </summary>
        public void ToggleTrailEffects(bool active)
        {
            if (_trailHierarchyRoot == null) return;

            if (active)
            {
                if (!_trailHierarchyRoot.isPlaying) 
                    _trailHierarchyRoot.Play(true); // 'true' includes children
            }
            else
            {
                if(_trailHierarchyRoot.isPlaying)
                {
                    // Stop emitting but allow existing particles to fade out naturally
                    _trailHierarchyRoot.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void ActivateBrushTip(Vector2 pos)
        {
            if (_brushTipParticles != null)
            {
                _brushTipParticles.transform.position = pos;
                _brushTipParticles.Play();
            }
        }

        private void DeactivateBrushTip()
        {
            if (_brushTipParticles != null) 
                _brushTipParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void SpawnBurstParticles(List<Vector2> points)
        {
            if (_finalizeBurstParticles == null || points.Count == 0) return;

            // EmitParams struct prevents allocating new objects for parameters
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                startColor = _burstColor,
                applyShapeToPosition = true
            };

            foreach (Vector2 pos in points)
            {
                emitParams.position = pos;
                _finalizeBurstParticles.Emit(emitParams, _particlesPerBurst);
            }
        }

        public static void SpawnBurstParticles(List<Vector2> points, ParticleSystem particleSystem, Color burstColor, int particlesPerBurst= 1)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                startColor = burstColor,
                applyShapeToPosition = true
            };
            foreach (Vector2 pos in points)
            {
                emitParams.position = pos;
                particleSystem.Emit(emitParams, particlesPerBurst);
            }
        }

        #endregion

        /// <summary>
        /// Fills the given list with the line's points transformed to world space.
        /// </summary>
        private void BuildWorldPointsFromLine(Line targetLine, List<Vector2> worldPointsOut)
        {
            worldPointsOut?.Clear();
            if (targetLine == null || targetLine.pointsCount < 2 || worldPointsOut == null) return;

            Transform lineTransform = targetLine.transform;
            for (int i = 0; i < targetLine.pointsCount; i++)
                worldPointsOut.Add(lineTransform.TransformPoint(targetLine.points[i]));
        }

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (_equidistantPointsBuffer == null || _equidistantPointsBuffer.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _equidistantPointsBuffer.Count; i++)
            {
                Gizmos.DrawSphere(_equidistantPointsBuffer[i], 0.1f);
                if (i < _equidistantPointsBuffer.Count - 1)
                    Gizmos.DrawLine(_equidistantPointsBuffer[i], _equidistantPointsBuffer[i + 1]);
            }
        }
        #endregion
    }
}
/*
using System.Collections.Generic;
using UnityEngine;
using Drawing.LineControl;

namespace Drawing.VFX
{
    public class LineVFXHandler : MonoBehaviour
    {
        [Header("Points Distribution Settings")]
        [Tooltip("If true, calculates points based on distance. If false, uses fixed number.")]
        [SerializeField] private bool _useSpacingMode = false;

        [Tooltip("Used if UseSpacingMode is FALSE: Fixed number of burst points.")]
        [SerializeField] private int _pointsNumber = 5;

        [Tooltip("Used if UseSpacingMode is TRUE: Minimum distance between burst points.")]
        [Min(0.01f)]
        [SerializeField] private float _minSpacing = 0.5f;
        
        [Header("Hierarchy Settings")]
        [Tooltip("The parent GameObject containing all drawing VFX (Trails, Sparkles, etc.)")]
        public ParticleSystem trailHierarchyRoot;
        [Header("Particle Systems")] [Tooltip("The main 'brush' effect that follows the tip.")]
        public ParticleSystem brushTipParticles;

        [SerializeField] private int particlesPerBurst = 15;
        [SerializeField] private Color startColor = Color.white;

        [Tooltip("Burst effect triggered when the line is finalized.")]
        public ParticleSystem finalizeBurstParticles;
        private Vector2 _lastSpawnPos;
        private float _distanceAccumulator;
        private LineManager _lineManager;
        private List<Vector2> _debugEquidistantPoints = new List<Vector2>();

        private void Awake()
        {
            _lineManager = GetComponent<LineManager>();
        }

        private void OnEnable()
        {
            // Subscribe to LineManager events to sync VFX with drawing state
            if (_lineManager != null)
            {
                _lineManager.OnLineStarted += HandleLineStarted;
                _lineManager.OnLineFinished += HandleLineFinished;
            }
        }

        private void OnDisable()
        {
            if (_lineManager != null)
            {
                _lineManager.OnLineStarted -= HandleLineStarted;
                _lineManager.OnLineFinished -= HandleLineFinished;
            }
        }

        private void HandleLineStarted(Line line)
        {
            // Position the root at the start of the line and enable effects
            ActivateBrushTipParticle(line);
            ToggleTrail(false);
            UpdateTrailPosition(line.transform.position);
            ToggleTrail(true);
        }



        

        /// <summary>
        /// Starts or stops all particle systems in the hierarchy.
        /// </summary>
        /// <param name="active">If true, Play() is called. If false, Stop() is called.</param>
        public void ToggleTrail(bool active)
        {
            if (trailHierarchyRoot == null) return;

            // Find all particle systems in children to ensure the entire hierarchy responds
            ParticleSystem particleSystem = trailHierarchyRoot.GetComponent<ParticleSystem>();

          
                if (active)
                {
                    
                    // Start the system if it's not already playing
                    if (!particleSystem.isPlaying) particleSystem.Play(true);
                }
                else
                {
                    // Stop emitting new particles, but let existing ones finish their lifetime (StopEmitting)
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            
        }

        /// <summary>
        /// Updates the world position of the entire VFX hierarchy to follow the drawing tip.
        /// </summary>
        public void UpdateTrailPosition(Vector2 pos)
        {
            if (trailHierarchyRoot != null)
            {
     
                trailHierarchyRoot.transform.position = pos;
                
            }
            if (brushTipParticles != null) brushTipParticles.transform.position = pos;
        }
        
        private void ActivateBrushTipParticle(Line line)
        {
            _lastSpawnPos = line.transform.position;
            _distanceAccumulator = 0f;

            if (brushTipParticles != null)
            {
                var main = brushTipParticles.main;
                brushTipParticles.transform.position = _lastSpawnPos;
                brushTipParticles.Play();
            }
        }

        private void DeactivateBrushTipParticle(Line line)
        {
            if (brushTipParticles != null) brushTipParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (finalizeBurstParticles != null)
            {
                finalizeBurstParticles.transform.position = line.GetLastPoint(); // In world space if needed

                // Fancy: Color the burst based on the line's gradient
                var main = finalizeBurstParticles.main;
                main.startColor = startColor;
                /*main.startColor = line.lineRenderer.colorGradient.Evaluate(1f);#1#

                finalizeBurstParticles.Play();
            }
        }
        
        /// <summary>
/// Calculates and returns a list of points distributed at equal distances along the line.
/// </summary>
/// <param name="targetLine">The line to sample points from.</param>
/// <param name="numberOfPoints">The desired number of equidistant points.</param>
/// <returns>A list of Vector2 points in world space.</returns>
public List<Vector2> GetEquidistantPointsOnLine(Line targetLine, int numberOfPoints)
{
    _debugEquidistantPoints.Clear();
    List<Vector2> equidistantPoints = new List<Vector2>();
    
    // We need at least 2 points to define a line segment
    if (targetLine == null || targetLine.pointsCount < 2 || numberOfPoints < 2)
    {
        return equidistantPoints;
    }

    // 1. Get all points from the line in world space
    List<Vector2> worldPoints = new List<Vector2>();
    List<Vector2> targetLocalPoints = new List<Vector2>(targetLine.points);
    for (int i = 0; i < targetLine.pointsCount; i++)
    {
        worldPoints.Add(targetLine.transform.TransformPoint((targetLocalPoints[i])));
    }

    // 2. Calculate the total length of the line
    float totalLength = 0f;
    float[] segmentLengths = new float[worldPoints.Count - 1];
    for (int i = 0; i < worldPoints.Count - 1; i++)
    {
        segmentLengths[i] = Vector2.Distance(worldPoints[i], worldPoints[i + 1]);
        totalLength += segmentLengths[i];
    }

    // 3. Calculate the step distance between each new point
    float stepDistance = totalLength / (numberOfPoints - 1);
    float currentTraveledDistance = 0f;
    int segmentIndex = 0;
    float distanceInCurrentSegment = 0f;

    // Add the very first point
    equidistantPoints.Add(worldPoints[0]);
    _debugEquidistantPoints.Add(worldPoints[0]);

    // 4. Interpolate to find the middle points
    for (int i = 1; i < numberOfPoints - 1; i++)
    {
        float targetDistance = i * stepDistance;

        // Move through segments until we reach the segment containing the target distance
        while (currentTraveledDistance + segmentLengths[segmentIndex] < targetDistance && segmentIndex < segmentLengths.Length - 1)
        {
            currentTraveledDistance += segmentLengths[segmentIndex];
            segmentIndex++;
        }

        // Calculate how far we are into the current segment
        float remainingDistanceNeeded = targetDistance - currentTraveledDistance;
        float t = remainingDistanceNeeded / segmentLengths[segmentIndex];

        // Linear interpolation between the two points of the segment
        Vector2 newPoint = Vector2.Lerp(worldPoints[segmentIndex], worldPoints[segmentIndex + 1], t);
        equidistantPoints.Add(newPoint);
        _debugEquidistantPoints.Add(newPoint);
    }

    // Add the very last point to ensure precision
    equidistantPoints.Add(worldPoints[worldPoints.Count - 1]);
    _debugEquidistantPoints.Add(worldPoints[worldPoints.Count - 1]);
    return equidistantPoints;
}
        
        private void OnDrawGizmos()
        {
            // Only draw if there are points to show
            if (_debugEquidistantPoints == null || _debugEquidistantPoints.Count == 0) return;

            // Set Gizmo color
            Gizmos.color = Color.red;

            for (int i = 0; i < _debugEquidistantPoints.Count; i++)
            {
                // Draw a small sphere at each equidistant point
                Gizmos.DrawSphere(_debugEquidistantPoints[i], 0.1f);

                // Optional: Draw a line between the equidistant points to visualize the path
                if (i < _debugEquidistantPoints.Count - 1)
                {
                    Gizmos.DrawLine(_debugEquidistantPoints[i], _debugEquidistantPoints[i + 1]);
                }
            }
        }
        
        


        private void HandleLineFinished(Line line)
        {
            List<Vector2> points = GetEquidistantPointsOnLine(line,_useSpacingMode, _pointsNumber, _minSpacing);
    
            SpawnParticlesAtPoints(points, line);

            DeactivateBrushTipParticle(line);
            ToggleTrail(false);
        }

        private void SpawnParticlesAtPoints(List<Vector2> points, Line line)
        {
            if (finalizeBurstParticles == null || points == null || points.Count == 0) return;

          
            /*Color burstColor = Color.white;
            if (line.lineRenderer != null)
            {
           
                burstColor = line.lineRenderer.colorGradient.Evaluate(Random.Range(0f, 1f)); 
            }#1#

          
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            /*
            emitParams.startColor = burstColor;#1#
            emitParams.startColor = startColor;
        
            foreach (Vector2 pos in points)
            {
                emitParams.position = pos; 
                emitParams.applyShapeToPosition = true; 
        
          
                finalizeBurstParticles.Emit(emitParams, particlesPerBurst);
            }
        }
        
        /// <summary>
        /// Calculates equidistant points. Can use fixed count OR minimum spacing distance.
        /// </summary>
        public List<Vector2> GetEquidistantPointsOnLine(Line targetLine, bool useSpacing, int fixedCount, float spacingDist)
        {
            _debugEquidistantPoints.Clear();
            List<Vector2> equidistantPoints = new List<Vector2>();
            
            if (targetLine == null || targetLine.pointsCount < 2)
            {
                return equidistantPoints;
            }

            // 1. Get all points and Calculate Total Length first
            List<Vector2> worldPoints = new List<Vector2>();
            List<Vector2> targetLocalPoints = new List<Vector2>(targetLine.points);
            
            float totalLength = 0f;
            
            // Convert to world and sum length
            for (int i = 0; i < targetLine.pointsCount; i++)
            {
                Vector2 wp = targetLine.transform.TransformPoint(targetLocalPoints[i]);
                worldPoints.Add(wp);

                if (i > 0)
                {
                    totalLength += Vector2.Distance(worldPoints[i-1], worldPoints[i]);
                }
            }
            
           
            float[] segmentLengths = new float[worldPoints.Count - 1];
            for (int i = 0; i < worldPoints.Count - 1; i++)
            {
                segmentLengths[i] = Vector2.Distance(worldPoints[i], worldPoints[i + 1]);
            }

            // 2. Determine Number of Points
            int finalPointCount = fixedCount;

            if (useSpacing && spacingDist > 0)
            {
      
                int segments = Mathf.FloorToInt(totalLength / spacingDist);
                finalPointCount = segments + 1;
            }

            // Safety check
            if (finalPointCount < 2) finalPointCount = 2;

            // 3. Calculate the exact step distance for equal distribution
            float stepDistance = totalLength / (finalPointCount - 1);
            
            float currentTraveledDistance = 0f;
            int segmentIndex = 0;

            equidistantPoints.Add(worldPoints[0]);
            _debugEquidistantPoints.Add(worldPoints[0]);

            // 4. Interpolate points
            for (int i = 1; i < finalPointCount - 1; i++)
            {
                float targetDistance = i * stepDistance;

                // Move forward in segments until we reach the target distance
                while (currentTraveledDistance + segmentLengths[segmentIndex] < targetDistance && segmentIndex < segmentLengths.Length - 1)
                {
                    currentTraveledDistance += segmentLengths[segmentIndex];
                    segmentIndex++;
                }

                float remainingDistanceNeeded = targetDistance - currentTraveledDistance;
                
                // Prevent division by zero if points are identical
                float t = (segmentLengths[segmentIndex] > 0.0001f) 
                    ? remainingDistanceNeeded / segmentLengths[segmentIndex] 
                    : 0f;

                Vector2 newPoint = Vector2.Lerp(worldPoints[segmentIndex], worldPoints[segmentIndex + 1], t);
                equidistantPoints.Add(newPoint);
                _debugEquidistantPoints.Add(newPoint);
            }

            // Add last point
            equidistantPoints.Add(worldPoints[worldPoints.Count - 1]);
            _debugEquidistantPoints.Add(worldPoints[worldPoints.Count - 1]);
            
            return equidistantPoints;
        }
    }
}
*/

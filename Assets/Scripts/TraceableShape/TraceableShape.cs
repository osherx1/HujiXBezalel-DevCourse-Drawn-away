using System.Collections.Generic;
using UnityEngine;
namespace TraceableShape
{
    
    public class TraceableShape : MonoBehaviour
    {
        [Header("Component References")] [SerializeField]
        private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D physicsBody;
        [SerializeField] private ParticleSystem revealEffect;
        [Header("Reveal Settings")] [SerializeField]
        private Sprite revealedSprite;
        [Tooltip("The percentage of points (0.0 to 1.0) that must be traced to reveal the shape.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float completionThreshold = 0.9f;
        private List<TracePoint> _tracePoints = new List<TracePoint>();
        private int _tracedPointsCount;
        private int _totalPoints;
        private bool _isRevealed;
        private void Start()
        {
            // Find all TracePoint components in children and subscribe to their event
            InitializeTracePoints();
        }
        private void InitializeTracePoints()
        {
            GetComponentsInChildren(true, _tracePoints);
            _totalPoints = _tracePoints.Count;

            if (_totalPoints == 0)
            {
                Debug.LogWarning("No TracePoints found in children. TraceableShape cannot be completed.", this);
                return;
            }

            foreach (var point in _tracePoints)
            {
                point.OnTraced += OnPointTraced;
            }
            // Ensure physics are disabled initially
            if (physicsBody != null)
            {
                physicsBody.simulated = false;
            }
        }
        private void OnEnable()
        {
            InitializeTracePoints();
        }

        private void OnDisable()
        {
            UnsubscribePointEvent();
        }
        private void OnDestroy()
        {
            UnsubscribePointEvent();
        }
        private void UnsubscribePointEvent()
        {
            // Unsubscribe to prevent memory leaks
            foreach (var point in _tracePoints)
            {
                if (point != null)
                {
                    point.OnTraced -= OnPointTraced;
                }
            }
        }
        private void OnPointTraced()
        {
            if (_isRevealed) return;

            _tracedPointsCount++;
            float currentProgress = (float)_tracedPointsCount / _totalPoints;

            if (currentProgress >= completionThreshold)
            {
                RevealObject();
            }
        }

        private void RevealObject()
        {
            _isRevealed = true;

            if (spriteRenderer != null && revealedSprite != null)
            {
                spriteRenderer.sprite = revealedSprite;
            }

            if (revealEffect != null)
            {
                revealEffect.Play();
            }

            if (physicsBody != null)
            {
                physicsBody.simulated = true;
            }

            // Optional: Unsubscribe from all points as we are now revealed
            foreach (var point in _tracePoints)
            {
                if (point != null)
                {
                    point.OnTraced -= OnPointTraced;
                }
            }
        }
    }
}
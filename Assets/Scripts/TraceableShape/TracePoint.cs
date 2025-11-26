namespace TraceableShape
{
    using System;
    using UnityEngine;
    [RequireComponent(typeof(Collider2D))]
    public class TracePoint : MonoBehaviour
    {
        public event Action OnTraced;

        [Tooltip("The tag of the object that should trigger this point (e.g., 'DrawingTool').")]
        [SerializeField] private string drawingToolTag = "Line";
        [SerializeField] Color traceColor = Color.green;
        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private bool _isTraced;
        private Color _originalColor;

        private void Awake()
        {
            TryGetComponent(out _collider);
            if (!_collider.isTrigger)
            {
                Debug.LogWarning(
                    $"Collider on {gameObject.name} is not set to 'Is Trigger'. TracePoint might not work correctly.",
                    this);
            }

            TryGetComponent(out _spriteRenderer);
            if (!_spriteRenderer)
            {
                Debug.LogWarning(
                    $"No SpriteRenderer found on {gameObject.name}. TracePoint visual feedback might not work correctly.",
                    this);
            }
            else
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            //Debug.Log($"TracePoint triggered by {other.gameObject.name}");
            if (_isTraced || !other.CompareTag(drawingToolTag))
            {
                return;
            }
            

            _isTraced = true;
            _collider.enabled = false;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {   
                spriteRenderer.color = traceColor;
            }
            OnTraced?.Invoke();
        }

        public void SetShowGizmos(bool showTracePointsGizmos)
        {
            _spriteRenderer.enabled = showTracePointsGizmos;
        }
        
        public void ResetPoint()
        {
            _isTraced = false;

            if (_collider != null)
            {
                _collider.enabled = true;
            }
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }
        }
    }
}
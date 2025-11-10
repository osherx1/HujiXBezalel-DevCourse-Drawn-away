namespace TraceableShape
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(Collider2D))]
    public class TracePoint : MonoBehaviour
    {
        public event Action OnTraced;

        [Tooltip("The tag of the object that should trigger this point (e.g., 'DrawingTool').")]
        [SerializeField] private string drawingToolTag = "DrawingTool";
        [SerializeField] Color traceColor = Color.green;
        private Collider2D _collider;
        private bool _isTraced;

        private void Awake()
        {
            TryGetComponent(out _collider);
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"Collider on {gameObject.name} is not set to 'Is Trigger'. TracePoint might not work correctly.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            print($"TracePoint triggered by {other.gameObject.name}");
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
    }
}
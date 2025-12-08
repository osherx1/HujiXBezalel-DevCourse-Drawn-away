using UnityEngine;
namespace Utilities
{
    public class TargetPositionTracker : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform tracker;
    
        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
        
        private void LateUpdate()
        {
            if (target == null || tracker == null) return;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector3 worldPosition = target.position + offset;
            tracker.position = worldPosition;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
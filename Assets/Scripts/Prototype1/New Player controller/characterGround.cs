using UnityEngine;

//This script is used by both movement and jump to detect when the character is touching the ground

public class characterGround : MonoBehaviour
{
        private bool onGround;
        private float distanceToGround = Mathf.Infinity;
       
        [Header("Collider Settings")]
        [SerializeField][Tooltip("Length of the ground-checking collider")] private float groundLength = 0.95f;
        [SerializeField][Tooltip("Extra distance to raycast beyond groundLength for early-landing detection")] private float groundSearchRange = 1f;
        [SerializeField][Tooltip("Distance between the ground-checking colliders")] private Vector3 colliderOffset;

        [Header("Layer Masks")]
        [SerializeField][Tooltip("Which layers are read as the ground")] private LayerMask groundLayer;
 

        private void FixedUpdate()
        {
            float hitA = CastForGround(transform.position + colliderOffset);
            float hitB = CastForGround(transform.position - colliderOffset);
            distanceToGround = Mathf.Min(hitA, hitB);
            onGround = distanceToGround <= groundLength;
        }

        private void OnDrawGizmos()
        {
            //Draw the ground colliders on screen for debug purposes
            if (onGround) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
            Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
            Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);
        }

        //Send ground detection to other scripts
        public bool GetOnGround() { return onGround; }
        public float GetDistanceToGround() { return distanceToGround; }
        public bool IsNearGround(float extraDistance)
        {
            return distanceToGround <= groundLength + Mathf.Max(extraDistance, 0f);
        }

        private float CastForGround(Vector3 origin)
        {
            float maxDistance = groundLength + Mathf.Max(groundSearchRange, 0f);
            var hit = Physics2D.Raycast(origin, Vector2.down, maxDistance, groundLayer);
            return hit ? hit.distance : Mathf.Infinity;
        }
}
using UnityEngine;

//This script is used by both movement and jump to detect when the character is touching the ground

public class characterGround : MonoBehaviour
{
        private bool onGround;
        private float distanceToGround = Mathf.Infinity;

    [Header("Detection Mode")]
    [SerializeField][Tooltip("If assigned, uses this collider's shape to cast down (more reliable on slopes/edges than rays).")]
    private Collider2D bodyCollider;

    [SerializeField][Tooltip("When true and bodyCollider is assigned, uses Collider2D.Cast for ground detection.")]
    private bool useColliderCast = true;

    [SerializeField][Tooltip("Max number of cast hits to consider (higher = safer but slightly more GC if you make it huge).")]
    private int maxCastHits = 8;
       
        [Header("Collider Settings")]
        [SerializeField][Tooltip("Length of the ground-checking collider")] private float groundLength = 0.95f;
        [SerializeField][Tooltip("Extra distance to raycast beyond groundLength for early-landing detection")] private float groundSearchRange = 1f;
        [SerializeField][Tooltip("Distance between the ground-checking colliders")] private Vector3 colliderOffset;

        [Header("Layer Masks")]
        [SerializeField][Tooltip("Which layers are read as the ground")] private LayerMask groundLayer;
 

        private void FixedUpdate()
        {
            distanceToGround = GetGroundDistance();
            onGround = distanceToGround <= groundLength;
        }

        private void Awake()
        {
            if (bodyCollider == null)
            {
                bodyCollider = GetComponentInParent<Collider2D>();
                if (bodyCollider == null)
                {
                    bodyCollider = GetComponent<Collider2D>();
                }
            }
        }

        private void OnDrawGizmos()
        {
            //Draw the ground colliders on screen for debug purposes
            if (onGround) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }

            if (useColliderCast && bodyCollider != null)
            {
                Bounds b = bodyCollider.bounds;
                Vector3 left = new Vector3(b.min.x, b.min.y, 0f);
                Vector3 right = new Vector3(b.max.x, b.min.y, 0f);
                Gizmos.DrawLine(left, left + Vector3.down * groundLength);
                Gizmos.DrawLine(right, right + Vector3.down * groundLength);
            }
            else
            {
                Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
                Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);
            }
        }

        //Send ground detection to other scripts
        public bool GetOnGround() { return onGround; }
        public float GetDistanceToGround() { return distanceToGround; }
        public bool IsNearGround(float extraDistance)
        {
            return distanceToGround <= groundLength + Mathf.Max(extraDistance, 0f);
        }

        private float GetGroundDistance()
        {
            float maxDistance = groundLength + Mathf.Max(groundSearchRange, 0f);

            if (useColliderCast && bodyCollider != null)
            {
                int hitCapacity = Mathf.Clamp(maxCastHits, 1, 64);
                RaycastHit2D[] hits = new RaycastHit2D[hitCapacity];

                ContactFilter2D filter = new ContactFilter2D();
                filter.useLayerMask = true;
                filter.layerMask = groundLayer;
                filter.useTriggers = false;

                int count = bodyCollider.Cast(Vector2.down, filter, hits, maxDistance);
                if (count <= 0)
                {
                    return Mathf.Infinity;
                }

                float min = Mathf.Infinity;
                for (int i = 0; i < count; i++)
                {
                    float d = hits[i].distance;
                    if (d < min)
                    {
                        min = d;
                    }
                }

                return min;
            }

            // Fallback: dual raycasts.
            float hitA = CastForGround(transform.position + colliderOffset);
            float hitB = CastForGround(transform.position - colliderOffset);
            return Mathf.Min(hitA, hitB);
        }

        private float CastForGround(Vector3 origin)
        {
            float maxDistance = groundLength + Mathf.Max(groundSearchRange, 0f);
            var hit = Physics2D.Raycast(origin, Vector2.down, maxDistance, groundLayer);
            return hit ? hit.distance : Mathf.Infinity;
        }
}
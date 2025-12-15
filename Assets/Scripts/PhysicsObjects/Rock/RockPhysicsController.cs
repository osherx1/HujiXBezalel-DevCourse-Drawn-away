using UnityEngine;

namespace Physics.Rock
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RockPhysicsController : MonoBehaviour
    {
        [Header("Thresholds")] [SerializeField]
        private float hardImpactThreshold = 5f;

        [SerializeField] private float shatterThreshold = 15f;

        [Header("Prefabs")] [SerializeField] private GameObject impactParticlePrefab;
        [SerializeField] private GameObject brokenRockPrefab; // Prefab with pieces

        private ImpactCalculator _impactCalculator;
        private Rigidbody2D _rb;
        [Header("Explosion Physics")]
        [SerializeField]private float explosionForce = 5f;
        [SerializeField]private float spinForce =10f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _impactCalculator = new ImpactCalculator(hardImpactThreshold, shatterThreshold);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            float impactVelocity = collision.relativeVelocity.magnitude;
            ContactPoint2D contact = collision.GetContact(0);

            // 1. Check for Shatter (High Priority)
            if (_impactCalculator.IsShatterImpact(impactVelocity) &&
                (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Line")))
            {
                HandleShatter(contact.point);
                return; // Stop here, object is destroyed
            }

            // 2. Check for Hard Impact (Low Priority)
            if (_impactCalculator.IsHardImpact(impactVelocity))
            {
                HandleHardImpact(contact.point);
            }
        }

        private void HandleHardImpact(Vector2 hitPosition)
        {
            if (impactParticlePrefab != null)
            {
                Instantiate(impactParticlePrefab, hitPosition, Quaternion.identity);
            }
        }

        private void HandleShatter(Vector2 hitPosition)
        {
            // Add impact effect even when shattering
            HandleHardImpact(hitPosition);

            if (brokenRockPrefab != null)
            {
                // Spawn the broken version at current position/rotation
                GameObject brokenInstance = Instantiate(brokenRockPrefab, transform.position, transform.rotation);

                // Apply current velocity to the broken pieces so they fly naturally
                ApplyExplosionPhysics(brokenInstance);
            }

            // Destroy the whole rock
            Destroy(gameObject);
        }

        private void ApplyMomentumToPieces(GameObject brokenRoot)
        {
            // Get all rigidbodies in the broken prefab children
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // Transfer velocity + add a little explosion force effect outward
                pieceRb.linearVelocity = _rb.linearVelocity;
                pieceRb.angularVelocity = _rb.angularVelocity;
            }
        }

        private void ApplyExplosionPhysics(GameObject brokenRoot)
        {
            Rigidbody2D[] pieces = brokenRoot.GetComponentsInChildren<Rigidbody2D>();
            Vector2 explosionCenter = transform.position;

            foreach (Rigidbody2D pieceRb in pieces)
            {
                // 1. Inherit Velocity
                pieceRb.linearVelocity = _rb.linearVelocity;

                // 2. Calculate Direction
                Vector2 direction = (pieceRb.position - explosionCenter).normalized;

                // 3. Add Randomness
                // Random.insideUnitCircle
                direction += Random.insideUnitCircle * 0.5f;

                // 4. Apply Explosion Force
                pieceRb.AddForce(direction * explosionForce, ForceMode2D.Impulse);

                // 5. Apply Random Spin
                float randomSpin = Random.Range(-1f, 1f) * spinForce;
                pieceRb.AddTorque(randomSpin, ForceMode2D.Impulse);
            }
        }
    }
}
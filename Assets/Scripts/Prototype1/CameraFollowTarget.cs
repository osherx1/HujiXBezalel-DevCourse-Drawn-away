using UnityEngine;

namespace Prototype1
{
    [RequireComponent(typeof(Transform))]
    public class CameraFollowTarget : MonoBehaviour
    {
        [Header("References")]
        public Transform player;                 // player's transform (assign in inspector)
        public Rigidbody2D playerRb;             // optional: to read velocity (better than input)
    
        [Header("Y lock")]
        public float fixedY = 0f;                // vertical position camera should keep
    
        [Header("Lookahead")]
        public float lookaheadDistance = 3f;     // how far ahead to show
        public float lookaheadSmoothing = 0.15f; // smoothing time for target movement
        public float directionSwitchThreshold = 0.05f; // velocity threshold to change lookahead direction
    
        [Header("Return")]
        public float returnDelay = 0.2f;         // delay before returning lookahead to center (optional)

        private Vector3 _velocity = Vector3.zero;
        private float _currentLookahead = 0f;
        private float _lookaheadDir = 1f;                // 1 = facing right, -1 = facing left
        private float _lastMoveTime = 0f;

        private void Reset()
        {
            // try to auto-assign common components
            if (player != null || Camera.main == null) return;
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }

        private void Start()
        {
            if (player == null)
                Debug.LogWarning("CameraFollowTarget: player not set.");
            if (playerRb == null && player != null)
            {
                playerRb = player.GetComponent<Rigidbody2D>();
            }
            // set initial Y to fixedY
            transform.position = new Vector3(player ? player.position.x : 0f, fixedY, transform.position.z);
        }

        private void Update()
        {
            if (player == null) return;

            // determine horizontal input direction: prefer Rigidbody2D velocity if available
            float hVel = 0f;
            hVel = playerRb != null ? playerRb.linearVelocity.x :
                // fallback: raw input (works if player uses Input.GetAxis)
                Input.GetAxisRaw("Horizontal");

            // if moving confidently, set lookahead direction
            if (Mathf.Abs(hVel) > directionSwitchThreshold)
            {
                _lookaheadDir = Mathf.Sign(hVel);
                _lastMoveTime = Time.time;
            }

            // compute desired lookahead amount (can return to 0 after delay)
            float targetLook = lookaheadDistance * _lookaheadDir;

            // optional: if player stopped, optionally shrink lookahead after returnDelay
            if (Mathf.Abs(hVel) <= directionSwitchThreshold && Time.time - _lastMoveTime > returnDelay)
            {
                targetLook = 0f;
            }

            // Smoothly damp the current lookahead
            _currentLookahead = Mathf.SmoothDamp(_currentLookahead, targetLook, ref _velocity.x, lookaheadSmoothing);

            // Target position for camera follow target: player's X + lookahead, fixed Y
            Vector3 targetPos = new Vector3(player.position.x + _currentLookahead, fixedY, transform.position.z);

            // Smoothly move the follow target to targetPos
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, lookaheadSmoothing);
        }
    }
}

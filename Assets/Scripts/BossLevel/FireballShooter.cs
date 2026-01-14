using UnityEngine;

public class FireballShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float shootInterval = 4.0f;
    [SerializeField] private float fireballSpeed = 10f;

    private float _timer;

    private void OnEnable()
    {
        _timer = shootInterval;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            ShootCone();
            _timer = shootInterval;
        }
    }

    private void ShootCone()
    {
        if (playerTarget == null) return;

        // Calculate direction to player
        Vector2 direction = (playerTarget.position - mouthPoint.position).normalized;

        // Angles: -15, 0, +15 degrees offset
        float[] angles = { -15f, 0f, 15f };

        foreach (float angle in angles)
        {
            // Rotate the direction vector
            Vector2 spreadDir = Quaternion.Euler(0, 0, angle) * direction;
            
            // Spawn
            GameObject ball = Instantiate(fireballPrefab, mouthPoint.position, Quaternion.identity);
            
            // Set Velocity (Assuming fireball has Rigidbody2D)
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.linearVelocity = spreadDir * fireballSpeed;
                // Align sprite rotation to velocity
                float rot_z = Mathf.Atan2(spreadDir.y, spreadDir.x) * Mathf.Rad2Deg;
                ball.transform.rotation = Quaternion.Euler(0f, 0f, rot_z);
            }
        }
    }
}
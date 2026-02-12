using UnityEngine;

public class RollingStone : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody2D rb;
    private AudioSource stoneAudio;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        stoneAudio = GetComponent<AudioSource>();
        ResetStoneImmediate();
    }

    public void ActivateStone()
    {
        // Force a reset first (in case it's currently at the bottom of the stairs)
        ResetStoneImmediate();

        // Then drop it
        rb.simulated = true;
        stoneAudio.Play();
        // Optional: Add a downward push if it sticks to the ceiling
        // rb.velocity = Vector2.down * 5f; 
    }

    private void ResetStoneImmediate()
    {
        rb.simulated = false;
        rb.linearVelocity = Vector2.zero; // Use .velocity in older Unity versions
        rb.angularVelocity = 0f;
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        if (stoneAudio) stoneAudio.Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Standard "Kill Floor" at the bottom
        if (other.CompareTag("ResetHole"))
        {
            ResetStoneImmediate();
        }
    }
}
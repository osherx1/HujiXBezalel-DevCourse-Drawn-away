using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float speed = 0.2f;          // מהירות תזוזה לצד
    public float leftLimit = -10f;      // נקודה שממנה הענן חוזר
    public float rightLimit = 10f;      // נקודת סיום

    [Header("Vertical Float")]
    public float floatAmplitude = 0.1f; // כמה למעלה/למטה
    public float floatSpeed = 0.5f;     // מהירות התנודה

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // תזוזה אופקית
        transform.position += Vector3.right * speed * Time.deltaTime;

        // תזוזה עדינה למעלה ולמטה
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(
            transform.position.x,
            startPosition.y + yOffset,
            transform.position.z
        );

        // לופ
        if (transform.position.x > rightLimit)
        {
            transform.position = new Vector3(
                leftLimit,
                transform.position.y,
                transform.position.z
            );
        }
    }
}

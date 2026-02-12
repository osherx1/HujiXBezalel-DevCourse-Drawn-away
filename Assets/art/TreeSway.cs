using UnityEngine;

public class TreeSway : MonoBehaviour
{
    public float swayAmount = 2f;      // כמה מעלות לכל צד
    public float swaySpeed = 1f;       // מהירות התנועה

    private float startRotation;

    void Start()
    {
        startRotation = transform.eulerAngles.z;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        transform.rotation = Quaternion.Euler(0, 0, startRotation + angle);
    }
}

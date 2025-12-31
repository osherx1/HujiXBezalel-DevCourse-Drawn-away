using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private Vector3 openOffset = new Vector3(0, 3, 0); // How far the door moves up
    [SerializeField] private float speed = 2.0f;

    private Vector3 closedPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        closedPosition = transform.position;
        targetPosition = closedPosition;
    }

    private void Update()
    {
        // Smoothly move the door to its target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    public void Open()
    {
        targetPosition = closedPosition + openOffset;
    }

    public void Close()
    {
        targetPosition = closedPosition;
    }
}
using UnityEngine;

public class StalactiteSpawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject stalactitePrefab;
    [SerializeField] private float spawnInterval = 2.0f;
    [SerializeField] private float spawnHeightY = 50f; // High above the map
    
    [Header("Channels")]
    [Tooltip("The X coordinates for the 4 falling channels")]
    [SerializeField] private float[] laneXCoordinates = new float[] { -5f, -2f, 2f, 5f };

    private float _timer;

    private void OnEnable()
    {
        _timer = spawnInterval;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            SpawnStalactite();
            _timer = spawnInterval;
        }
    }

    private void SpawnStalactite()
    {
        // Pick a random lane
        int laneIndex = Random.Range(0, laneXCoordinates.Length);
        float xPos = laneXCoordinates[laneIndex];

        Vector3 spawnPos = new Vector3(xPos, spawnHeightY, 0);
        Instantiate(stalactitePrefab, spawnPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (float x in laneXCoordinates)
        {
            Gizmos.DrawLine(new Vector3(x, spawnHeightY, 0), new Vector3(x, 0, 0));
        }
    }
}
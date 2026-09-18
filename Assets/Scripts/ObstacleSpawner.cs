using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public float spawnInterval = 1.5f;
    public float[] lanePositions = { -6.0f, -3.0f, 0f, 3.0f, 6.0f };

    private float timer;

    void Update()
    {
        // Only spawn obstacles when road is actually moving
        if (RoadScroller.CurrentSpeed < 0.5f) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        float randomLaneX = lanePositions[Random.Range(0, lanePositions.Length)];
        Vector3 spawnPosition = new Vector3(randomLaneX, transform.position.y, 0);
        Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
    }
}
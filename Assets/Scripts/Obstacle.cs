using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        // Move at the same speed as the road
        transform.position += Vector3.down * RoadScroller.CurrentSpeed * Time.deltaTime;

        // Destroy when off the bottom of the screen
        if (transform.position.y < -8f)
            Destroy(gameObject);
    }
}
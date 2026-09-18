using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    public Transform otherTile;
    public float tileHeight = 8f;

    // Current scroll speed — controlled by NeuroRacesConnector via pow data
    public static float CurrentSpeed = 0f;
    public static float maxScrollSpeed = 6f;

    void Update()
    {
        transform.position += Vector3.down * CurrentSpeed * Time.deltaTime;

        if (transform.position.y < -tileHeight)
            Reposition();
    }

    void Reposition()
    {
        if (otherTile == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = new Vector3(
            transform.position.x,
            otherTile.position.y + tileHeight,
            transform.position.z
        );
    }
}
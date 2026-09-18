using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CarController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float leftBoundary  = -6.2f;
    public float rightBoundary = 6.2f;
    public string currentCommand = "neutral";

    private bool isInvincible = false;

    void Update()
    {
        // Choose direction source based on settings
        string direction;

        if (GameSettings.SelectedDirectionMode == GameSettings.DirectionMode.HeadRotation)
            direction = HeadRotationController.CurrentCommand;
        else
            direction = NeuroRacesConnector.CurrentCommand;

        // Keyboard fallback for testing
        var kb = Keyboard.current;
        if (kb.leftArrowKey.isPressed)  direction = "left";
        if (kb.rightArrowKey.isPressed) direction = "right";

        currentCommand = direction;

        Vector3 movement = Vector3.zero;
        if (currentCommand == "left")
            movement = Vector3.left * moveSpeed * Time.deltaTime;
        else if (currentCommand == "right")
            movement = Vector3.right * moveSpeed * Time.deltaTime;

        transform.position += movement;

        float clampedX = Mathf.Clamp(transform.position.x, leftBoundary, rightBoundary);
        transform.position = new Vector3(clampedX, transform.position.y, 0);
    }

    // Detect collision with obstacles
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision with: " + other.gameObject.name +
                  " tag: " + other.gameObject.tag);

        if (other.CompareTag("Obstacle") && !isInvincible)
        {
            TakeHit();
        }
    }

    void TakeHit()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }

        Debug.Log("Hit! Lives before: " + GameManager.Instance.lives);
        GameManager.Instance.LoseLife();
        StartCoroutine(InvincibilityFrames());
    }

    // Flash the car and grant brief invincibility after a hit
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        for (int i = 0; i < 6; i++)
        {
            sr.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.12f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.12f);
        }

        isInvincible = false;
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Singleton so any script can access GameManager.Instance
    public static GameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public GameObject gameOverPanel;

    [Header("Game State")]
    public int lives = 3;
    public float score = 0f;
    private bool isGameOver = false;

    void Awake()
    {
        // Singleton setup
        Instance = this;
    }

    void Start()
    {
        // Hide game over panel at the start
        gameOverPanel.SetActive(false);
        UpdateUI();
    }

    void Update()
    {
        if (isGameOver) return;

        // Score only increases when player is focused and road is moving
        if (RoadScroller.CurrentSpeed > 0.5f)
        {
            score += Time.deltaTime * RoadScroller.CurrentSpeed * 10f;
        }

        UpdateUI();
    }

    public void LoseLife()
    {
        lives--;
        UpdateUI();

        // Log hit event to CSV
        if (CSVLogger.Instance != null)
            CSVLogger.Instance.LogEvent("HIT");

        if (lives <= 0)
            TriggerGameOver();
    }

    void TriggerGameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);

        // Save CSV when game ends
        if (CSVLogger.Instance != null)
            CSVLogger.Instance.LogEvent("GAME_OVER");
            CSVLogger.Instance.SaveFile();
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + Mathf.FloorToInt(score);
        livesText.text = "Lives: " + lives;
    }

    // Called by the Restart button
    public void RestartGame()
    {
        Time.timeScale = 1f;
        // Go back to main menu instead of reloading game directly
        SceneManager.LoadScene("MainMenu");
    }
}
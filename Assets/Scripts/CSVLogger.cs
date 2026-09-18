using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using EmotivUnityPlugin;

public class CSVLogger : MonoBehaviour
{
    public static CSVLogger Instance;

    private string filePath;
    private StringBuilder buffer = new StringBuilder();
    private bool isLogging = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Save file to Desktop with timestamp in filename
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName  = $"NeuroRaces_{timestamp}.csv";
        filePath = Path.Combine(
            System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Desktop),
            fileName
        );

        // Write CSV header
        buffer.AppendLine(
            "Time," +
            "Score," +
            "Lives," +
            "Command," +
            "RoadSpeed," +
            "IsMoving," +
            "Alpha_AF3," +
            "LowBeta_AF3," +
            "HighBeta_AF3," +
            "Alpha_AF4," +
            "LowBeta_AF4," +
            "HighBeta_AF4," +
            "Event"
        );

        isLogging = true;
        StartCoroutine(LogLoop());
        Debug.Log("[CSV] Logging to: " + filePath);
    }

    // Log one row every 0.5 seconds during gameplay
    private IEnumerator LogLoop()
    {
        while (isLogging)
        {
            WriteRow("");
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Call this for special events — hit, game over etc
    public void LogEvent(string eventName)
    {
        WriteRow(eventName);
    }

    private void WriteRow(string eventName)
    {
        if (GameManager.Instance == null) return;

        string time      = System.DateTime.Now.ToString("HH:mm:ss.fff");
        float  score     = GameManager.Instance.score;
        int    lives     = GameManager.Instance.lives;
        string command   = NeuroRacesConnector.CurrentCommand;
        float  speed     = RoadScroller.CurrentSpeed;
        bool   isMoving  = NeuroRacesConnector.IsMoving;

        // EEG band power values
        double alphaAF3   = DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF3);
        double lowBetaAF3 = DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF3);
        double hiBetaAF3  = DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_AF3);
        double alphaAF4   = DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF4);
        double lowBetaAF4 = DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF4);
        double hiBetaAF4  = DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_AF4);

        buffer.AppendLine(
            $"{time}," +
            $"{score:F1}," +
            $"{lives}," +
            $"{command}," +
            $"{speed:F2}," +
            $"{isMoving}," +
            $"{alphaAF3:F4}," +
            $"{lowBetaAF3:F4}," +
            $"{hiBetaAF3:F4}," +
            $"{alphaAF4:F4}," +
            $"{lowBetaAF4:F4}," +
            $"{hiBetaAF4:F4}," +
            $"{eventName}"
        );
    }

    // Save buffer to file — called on game over and on quit
    public void SaveFile()
    {
        isLogging = false;
        File.WriteAllText(filePath, buffer.ToString());
        Debug.Log("[CSV] File saved: " + filePath);
    }

    void OnApplicationQuit()
    {
        SaveFile();
    }
}
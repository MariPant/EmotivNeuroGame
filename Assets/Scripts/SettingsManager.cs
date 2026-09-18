using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using EmotivUnityPlugin;

public class SettingsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelConnecting;
    public GameObject panelDirection;
    public GameObject panelSpeed;
    public GameObject panelCalibration;

    [Header("Connecting Panel")]
    public TextMeshProUGUI statusText;

    [Header("Calibration Panel")]
    public TextMeshProUGUI calibrationText;
    public Slider           calibrationProgress;
    public Button           startButton;

    private string currentHeadsetId = "";
    private bool   sessionReady     = false;
    private bool   sessionRequested = false;
    void Start()
    {
        ShowPanel(panelConnecting);
        startButton.interactable = false;

        try
        {
            EmotivUnityItf.Instance.Init(
                AppCredentials.ClientId,
                AppCredentials.ClientSecret,
                AppCredentials.AppName,
                true
            );
            EmotivUnityItf.Instance.Start();
            statusText.text = "Searching for headset...";
            StartCoroutine(HeadsetFlow());
        }
        catch (System.Exception ex)
        {
            statusText.text = "Error: " + ex.Message;
        }
    }

    private IEnumerator HeadsetFlow()
    {
        yield return new WaitForSeconds(8f);

        int attempts = 0;
        while (!sessionReady && attempts < 15)
        {
            attempts++;
            statusText.text = $"Connecting... ({attempts}/15)";
            EmotivUnityItf.Instance.QueryHeadsets();
            yield return new WaitForSeconds(3f);
            TryConnect();
            yield return new WaitForSeconds(4f);
        }

        if (!sessionReady)
        {
            statusText.text = "Headset not found.\nContinuing without headset.";
            GameSettings.HeadsetConnected = false;
            yield return new WaitForSeconds(2f);
            ShowPanel(panelDirection);
        }
    }

    private void TryConnect()
    {
        if (sessionReady) return;

        var headsets = EmotivUnityItf.Instance.GetDetectedHeadsets();
        if (headsets == null || headsets.Count == 0) return;

        if (string.IsNullOrEmpty(currentHeadsetId))
        {
            foreach (var hs in headsets)
            {
                if (hs.HeadsetID != null && hs.HeadsetID.ToUpper().Contains("EPOC"))
                {
                    currentHeadsetId = hs.HeadsetID;
                    statusText.text = "Headset found: " + currentHeadsetId;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(currentHeadsetId))
                EmotivUnityItf.Instance.CreateSessionWithHeadset(currentHeadsetId);
            return;
        }

        if (!sessionRequested)
        {
            sessionRequested = true;
            return;
        }

        if (EmotivUnityItf.Instance.IsSessionCreated)
            StartCoroutine(SubscribeAndProceed());
    }

    private IEnumerator SubscribeAndProceed()
    {
        sessionReady = true;
        GameSettings.HeadsetConnected = true;

        EmotivUnityItf.Instance.SubscribeData(
            new System.Collections.Generic.List<string> { "pow", "com", "sys", "mot" }
        );

        statusText.text = "Connected! ✓";
        yield return new WaitForSeconds(1.5f);
        ShowPanel(panelDirection);
    }

    // --- DIRECTION BUTTONS ---

    public void SelectMentalCommands()
    {
        GameSettings.SelectedDirectionMode = GameSettings.DirectionMode.MentalCommands;
        ShowPanel(panelSpeed);
    }

    public void SelectHeadRotation()
    {
        GameSettings.SelectedDirectionMode = GameSettings.DirectionMode.HeadRotation;
        ShowPanel(panelSpeed);
    }

    // --- SPEED BUTTONS ---

    public void SelectPOW()
    {
        GameSettings.SelectedSpeedMode = GameSettings.SpeedMode.POW;
        ShowPanel(panelCalibration);
        StartCoroutine(CalibrationFlow());
    }

    public void SelectKeyboard()
    {
        GameSettings.SelectedSpeedMode = GameSettings.SpeedMode.Keyboard;
        calibrationText.text = "Ready! Press START to begin.";
        calibrationProgress.value = 1f;
        startButton.interactable = true;
        ShowPanel(panelCalibration);
    }
    
    // --- CALIBRATION ---

    private IEnumerator CalibrationFlow()
    {
        startButton.interactable = false;
        calibrationProgress.value = 0f;

        // Phase 1 — relaxed baseline
        calibrationText.text = "Relax and close your eyes...\n(5 seconds)";

        float relaxAlpha = 0f;
        float relaxBeta  = 0f;
        int   samples    = 0;
        float timer      = 0f;

        while (timer < 5f)
        {
            timer += Time.deltaTime;
            calibrationProgress.value = timer / 10f;
            relaxAlpha += (float)DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF3);
            relaxBeta  += (float)DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF3);
            samples++;
            yield return null;
        }

        // Phase 2 — focused
        calibrationText.text = "Now focus!\nThink actively about something...\n(5 seconds)";

        float focusBeta  = 0f;
        float focusAlpha = 0f;
        int   samples2   = 0;
        timer = 0f;

        while (timer < 5f)
        {
            timer += Time.deltaTime;
            calibrationProgress.value = 0.5f + timer / 10f;
            focusBeta  += (float)DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF3);
            focusAlpha += (float)DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF3);
            samples2++;
            yield return null;
        }

        if (samples  > 0) GameSettings.BaselineAlpha = relaxAlpha / samples;
        if (samples2 > 0) GameSettings.BaselineBeta  = focusBeta  / samples2;

        calibrationText.text =
            $"Calibration complete!\n" +
            $"Alpha (relaxed): {GameSettings.BaselineAlpha:F2}\n" +
            $"Beta (focused):  {GameSettings.BaselineBeta:F2}\n\n" +
            $"Press START!";

        calibrationProgress.value = 1f;
        startButton.interactable = true;
    }

    // --- START GAME ---

    public void StartGame()
    {
        // Load game scene
        SceneManager.LoadScene("Game");
    }

    // --- HELPERS ---

    private void ShowPanel(GameObject panel)
    {
        panelConnecting.SetActive(false);
        panelDirection.SetActive(false);
        panelSpeed.SetActive(false);
        panelCalibration.SetActive(false);

        panel.SetActive(true);
    }
}
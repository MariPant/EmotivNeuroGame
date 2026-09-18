using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignalIndicatorUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider          signalSlider;
    public TextMeshProUGUI commandText;
    public TextMeshProUGUI powerText;

    [Header("Slider Fill Colors")]
    public Image sliderFill; // drag the Fill image of the slider here

    // Colors for each state
    private Color colorLeft    = new Color(1f,   0.27f, 0.27f); // red
    private Color colorNeutral = new Color(1f,   0.85f, 0f);    // yellow
    private Color colorRight   = new Color(0.27f,1f,   0.27f);  // green

    private float smoothedValue = 0f;

    void Update()
    {
        string command = GetCurrentCommand();
        float  power   = GetCurrentPower();

        // Convert command + power to slider value (-1 to +1)
        float targetValue = 0f;
        if (command == "left")  targetValue = -power;
        if (command == "right") targetValue =  power;

        // Smooth movement
        smoothedValue = Mathf.Lerp(smoothedValue, targetValue, 0.2f);
        signalSlider.value = smoothedValue;

        // Update text
        commandText.text = command.ToUpper();
        powerText.text   = $"PWR: {power:F2}";

        // Update fill color based on direction
        if (sliderFill != null)
        {
            if (command == "left")       sliderFill.color = colorLeft;
            else if (command == "right") sliderFill.color = colorRight;
            else                         sliderFill.color = colorNeutral;
        }
    }

    private string GetCurrentCommand()
    {
        if (GameSettings.SelectedDirectionMode == GameSettings.DirectionMode.HeadRotation)
            return HeadRotationController.CurrentCommand;
        else
            return NeuroRacesConnector.CurrentCommand;
    }

    private float GetCurrentPower()
    {
        return NeuroRacesConnector.CurrentPower;
    }
}
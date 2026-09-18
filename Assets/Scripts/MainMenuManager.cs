using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject bandPowerPanel;

    // --- BUTTONS ---

    public void OnSettingsClick()
    {
        SceneManager.LoadScene("SettingScene");
    }

    public void OnBandPowerInfoClick()
    {
        // Show band power info panel
        bandPowerPanel.SetActive(true);
    }

    public void OnCloseBandPowerClick()
    {
        // Hide band power info panel
        bandPowerPanel.SetActive(false);
    }

    public void OnBandPowerClick()
    {
        SceneManager.LoadScene("PowScene");
    }

    public void OnExitClick()
    {
        Debug.Log("[MainMenu] Exit");
        Application.Quit();
    }
}
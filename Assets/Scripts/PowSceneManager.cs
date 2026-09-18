using UnityEngine;
using UnityEngine.SceneManagement;
using EmotivUnityPlugin;
using System.Collections;
using System.Collections.Generic;

public class PowSceneManager : MonoBehaviour
{
    [Header("Status")]
    public TMPro.TextMeshProUGUI statusText;

    private string currentHeadsetId = "";
    private bool   subscribed       = false;
    private bool   sessionRequested = false;

    void Start()
    {
        try
        {
            EmotivUnityItf.Instance.Init(
                AppCredentials.ClientId,
                AppCredentials.ClientSecret,
                AppCredentials.AppName,
                true
            );
            EmotivUnityItf.Instance.Start();

            if (statusText != null)
                statusText.text = "Connecting...";

            StartCoroutine(HeadsetFlow());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[PowScene] Init failed: " + ex.Message);
        }
    }

    private IEnumerator HeadsetFlow()
    {
        yield return new WaitForSeconds(8f);

        int attempts = 0;
        while (!subscribed && attempts < 15)
        {
            attempts++;
            EmotivUnityItf.Instance.QueryHeadsets();
            yield return new WaitForSeconds(3f);
            TryConnect();
            yield return new WaitForSeconds(4f);
        }

        if (!subscribed && statusText != null)
            statusText.text = "Headset not found";
    }

    private void TryConnect()
    {
        if (subscribed) return;

        var headsets = EmotivUnityItf.Instance.GetDetectedHeadsets();
        if (headsets == null || headsets.Count == 0) return;

        if (string.IsNullOrEmpty(currentHeadsetId))
        {
            foreach (var hs in headsets)
            {
                if (hs.HeadsetID != null && hs.HeadsetID.ToUpper().Contains("EPOC"))
                {
                    currentHeadsetId = hs.HeadsetID;
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

        if (EmotivUnityItf.Instance.IsSessionCreated && !subscribed)
            StartCoroutine(SubscribeAndStart());
    }

    private IEnumerator SubscribeAndStart()
    {
        subscribed = true;
        EmotivUnityItf.Instance.SubscribeData(
            new List<string> { "pow" }
        );

        yield return new WaitForSeconds(1f);

        if (statusText != null)
            statusText.text = "Connected ✓";

        Debug.Log("[PowScene] Streaming pow data");
    }

    // Called by Main Menu button in PowScene
    public void GoToMainMenu()
    {
        EmotivUnityItf.Instance.Stop();
        SceneManager.LoadScene("MainMenu");
    }
}
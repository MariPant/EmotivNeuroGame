using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; 
using EmotivUnityPlugin;

public class NeuroRacesConnector : MonoBehaviour
{
    [Header("Emotiv App Credentials")]
    [SerializeField] private string profileName  = "NeuroRacesProfile";

    [Header("Speed Settings")]
    // Threshold for beta/alpha ratio:
    // if exceeded → user is focused → car moves
    [SerializeField] private float betaAlphaThreshold = 1.2f;

    // Normalization factor for EEG values
    [SerializeField] private float normalizationDivisor = 10f;

    // Number of samples used for smoothing (moving average)
    [SerializeField] private int smoothingWindow = 3;

    // Interval between EEG updates
    [SerializeField] private float updateInterval = 0.1f;

    [Header("Steering Settings")]
    // Minimum mental command power to be accepted
    [SerializeField] private float commandPowerThreshold = 0.3f;    

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;
    [SerializeField] private bool testModeWithoutHeadset = true; 

    // Shared values used by game logic (e.g. CarController)
    public static string CurrentCommand = "neutral"; // "left", "right", "neutral"
    public static bool IsMoving = false;             // true = user is focused → car moves
    public static float  CurrentPower   = 0f;

    private string currentHeadsetId = "";
    private bool subscribed = false;

    // Queues for smoothing beta and alpha signals
    private readonly Queue<float> betaQueue  = new Queue<float>();
    private readonly Queue<float> alphaQueue = new Queue<float>();

    private float smoothedBeta  = 0f;
    private float smoothedAlpha = 0f;

    private void Start()
    {
        // Skip all Emotiv code — test game with keyboard only
        if (testModeWithoutHeadset)
        {
            Debug.Log("[NeuroRaces] TEST MODE — keyboard only, Emotiv disabled");
            StartCoroutine(TestModeLoop());
            return;
        }

        try
        {
            Debug.Log("[NeuroRaces] Initializing...");

            // Initialize Emotiv API
            EmotivUnityItf.Instance.Init(
                AppCredentials.ClientId,
                AppCredentials.ClientSecret,
                AppCredentials.AppName,
                true
            );

            // Start Emotiv service
            EmotivUnityItf.Instance.Start();

            // Begin headset detection and connection flow
            StartCoroutine(HeadsetFlow());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NeuroRaces] Init failed: " + ex.Message);
        }
    }
    private float GetDynamicThreshold()
    {
        if (GameSettings.BaselineBeta > 0 && GameSettings.BaselineAlpha > 0)
            // Threshold = ratio at rest + 20% buffer
            return (GameSettings.BaselineBeta / GameSettings.BaselineAlpha) * 1.2f;
        else
            return betaAlphaThreshold; // fallback to Inspector value
    }


    private IEnumerator TestModeLoop()
    {
        while (true)
        {
            var kb = Keyboard.current;

            // Spacebar held = focused (beta) = road moves
            if (kb.spaceKey.isPressed)
            {
                RoadScroller.CurrentSpeed = Mathf.Lerp(
                    RoadScroller.CurrentSpeed, 6f, 0.1f);
                IsMoving = true;
            }
            else
            {
                RoadScroller.CurrentSpeed = Mathf.Lerp(
                    RoadScroller.CurrentSpeed, 0f, 0.05f);
                IsMoving = RoadScroller.CurrentSpeed > 0.1f;
            }

            // Arrow keys = mental commands
            if (kb.leftArrowKey.isPressed)
                CurrentCommand = "left";
            else if (kb.rightArrowKey.isPressed)
                CurrentCommand = "right";
            else
                CurrentCommand = "neutral";

            yield return null;
        }
    }

    // Main coroutine responsible for detecting and connecting headset
    private IEnumerator HeadsetFlow()
    {
        Debug.Log("[NeuroRaces] Waiting for Cortex service...");
        yield return new WaitForSeconds(8f);

        int maxAttempts = 15;
        int attempts = 0;

        while (!subscribed && attempts < maxAttempts)
        {
            attempts++;
            Debug.Log($"[NeuroRaces] Attempt {attempts}/{maxAttempts}");

            EmotivUnityItf.Instance.QueryHeadsets();

            // Give Cortex time to respond before we read the result
            yield return new WaitForSeconds(3f);

            TryStartStreams();

            // Give session creation time to complete before next attempt
            yield return new WaitForSeconds(4f);
        }

        if (!subscribed)
        {
            Debug.LogWarning("[NeuroRaces] Could not connect. Falling back to keyboard.");
            StartCoroutine(TestModeLoop());
        }
    }

    private bool sessionRequested = false;

    private void TryStartStreams()
    {
        if (subscribed) return;

        // --- STEP 1: find headset and request session ---
        if (string.IsNullOrEmpty(currentHeadsetId))
        {
            List<Headset> headsets = null;
            try { headsets = EmotivUnityItf.Instance.GetDetectedHeadsets(); }
            catch (System.Exception ex)
            {
                Debug.LogError("[NeuroRaces] GetDetectedHeadsets error: " + ex.Message);
                return;
            }

            if (headsets == null || headsets.Count == 0)
            {
                Debug.Log("[NeuroRaces] No headsets yet...");
                return;
            }

            foreach (var hs in headsets)
            {
                Debug.Log($"[NeuroRaces] Headset: {hs.HeadsetID} | Status: {hs.Status}");
                if (hs.HeadsetID != null && hs.HeadsetID.ToUpper().Contains("EPOC"))
                {
                    currentHeadsetId = hs.HeadsetID;
                    break;
                }
            }

            if (string.IsNullOrEmpty(currentHeadsetId))
            {
                Debug.Log("[NeuroRaces] EPOC not found.");
                return;
            }

            Debug.Log("[NeuroRaces] Requesting session for: " + currentHeadsetId);
            sessionRequested = false;
            return;
        }

        // --- STEP 2: request session once ---
        if (!sessionRequested)
        {
            try
            {
                EmotivUnityItf.Instance.CreateSessionWithHeadset(currentHeadsetId);
                sessionRequested = true;
                Debug.Log("[NeuroRaces] CreateSession called — waiting for activation...");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[NeuroRaces] CreateSession error: " + ex.Message);
                currentHeadsetId = "";
                sessionRequested = false;
            }
            return; // always return here — let next attempt check if session is ready
        }

        // --- STEP 3: check session is truly active via MessageLog ---
        // IsSessionCreated becomes true only after full activation
        if (!EmotivUnityItf.Instance.IsSessionCreated)
        {
            Debug.Log("[NeuroRaces] Waiting for session activation...");
            return;
        }

        // Extra safety — wait one more cycle after IsSessionCreated
        // to make sure the internal state is fully ready before subscribing
        if (!subscribed)
        {
            Debug.Log("[NeuroRaces] Session active! Starting subscribe coroutine...");
            StartCoroutine(SubscribeAfterDelay());
            subscribed = true; // set true now to prevent duplicate calls
        }
    }

    // Subscribe in a separate coroutine with a small extra delay
    // This gives the plugin time to fully process the session activation
    private IEnumerator SubscribeAfterDelay()
    {
        // TEMPORARY TEST — subscribe to pow and see if Cortex accepts it
        EmotivUnityItf.Instance.SubscribeData(
            new List<string> { "com", "sys", "pow" }
        );
        Debug.Log("[NeuroRaces] Waiting 2s before subscribing...");
        yield return new WaitForSeconds(2f);

        try
        {
            Debug.Log("[NeuroRaces] Subscribing to pow + com + sys + mot...");
            EmotivUnityItf.Instance.SubscribeData(
                new List<string> { "pow", "com", "sys", "mot" }
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NeuroRaces] SubscribeData error: " + ex.Message);
            yield break;
        }

        yield return new WaitForSeconds(1f);

        try
        {
            EmotivUnityItf.Instance.LoadProfile(profileName);
            Debug.Log("[NeuroRaces] Profile load requested: " + profileName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NeuroRaces] LoadProfile error: " + ex.Message);
        }

        // Start data loops only after subscribe is done
        StartCoroutine(UpdatePowLoop());
        StartCoroutine(UpdateComLoop());
        Debug.Log("[NeuroRaces] All streams running!");
    }

    // ----------- STREAM 1: Beta/Alpha → movement (speed logic) -----------
    private IEnumerator UpdatePowLoop()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            // Collect beta and alpha from all three channel pairs
            // as defined in the presentation (AF, F, FC zones)
            double beta =
                (DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF3)
            + DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_AF3)
            + DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF4)
            + DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_AF4)) * 0.5 +
                (DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_F3)
            + DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_F3)
            + DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_F4)
            + DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_F4)) * 0.35 +
                (DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_FC5)
            + DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_FC5)
            + DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_FC6)
            + DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_FC6)) * 0.15;

            double alpha =
                DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF3) * 0.5 +
                DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF4) * 0.5 +
                DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_F3)  * 0.35 +
                DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_F4)  * 0.35 +
                DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_FC5) * 0.15 +
                DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_FC6) * 0.15;

            // Smoothing — same as EmotivConnector
            float betaNorm  = Mathf.Pow(Mathf.Clamp01((float)(beta  / 5f)), 0.7f);
            float alphaNorm = Mathf.Pow(Mathf.Clamp01((float)(alpha / 5f)), 0.7f);

            betaQueue.Enqueue(betaNorm);
            alphaQueue.Enqueue(alphaNorm);
            while (betaQueue.Count  > smoothingWindow) betaQueue.Dequeue();
            while (alphaQueue.Count > smoothingWindow) alphaQueue.Dequeue();

            float avgBeta = 0f, avgAlpha = 0f;
            foreach (float v in betaQueue)  avgBeta  += v;
            foreach (float v in alphaQueue) avgAlpha += v;
            if (betaQueue.Count  > 0) avgBeta  /= betaQueue.Count;
            if (alphaQueue.Count > 0) avgAlpha /= alphaQueue.Count;

            smoothedBeta  = Mathf.Lerp(smoothedBeta,  avgBeta,  0.15f);
            smoothedAlpha = Mathf.Lerp(smoothedAlpha, avgAlpha, 0.15f);

            // Beta dominates → focused → road moves
            // Alpha dominates → relaxed → road slows down
            float ratio = (smoothedAlpha > 0.05f) ? smoothedBeta / smoothedAlpha : 0f;
            float targetSpeed = 0f;

            if (ratio >= GetDynamicThreshold() && smoothedBeta > 0.05f)
            {
                targetSpeed = Mathf.Clamp(
                    (ratio - betaAlphaThreshold) * 4f,
                    0f,
                    RoadScroller.maxScrollSpeed
                );
                IsMoving = true;
            }
            else
            {
                IsMoving = false;
            }

            RoadScroller.CurrentSpeed = Mathf.Lerp(
                RoadScroller.CurrentSpeed,
                targetSpeed,
                IsMoving ? 0.1f : 0.05f
            );

            if (verboseLogging)
                Debug.Log($"[NeuroRaces] Beta:{smoothedBeta:F3} Alpha:{smoothedAlpha:F3} " +
                        $"Ratio:{ratio:F2} Speed:{RoadScroller.CurrentSpeed:F2}");

            yield return new WaitForSeconds(updateInterval);
        }
    }

    // ----------- STREAM 2: Mental command → steering -----------
    private IEnumerator UpdateComLoop()
    {
        while (true)
        {
            // Get latest mental command
            MentalComm mc = EmotivUnityItf.Instance.LatestMentalCommand;

            // Check if command is strong enough
            if (mc != null && mc.pow >= commandPowerThreshold) {
                CurrentCommand = mc.act; // "left", "right", "neutral"
                CurrentPower   = (float)mc.pow;
            }
            else
                CurrentCommand = "neutral";
                CurrentPower   = 0f;  

            if (verboseLogging)
                Debug.Log($"[NeuroRaces] Command:{CurrentCommand} Power:{mc?.pow:F2}");

            yield return null;
        }
    }

    private void OnDestroy()
    {
        // Stop Emotiv service when object is destroyed
        EmotivUnityItf.Instance.Stop();
    }
}
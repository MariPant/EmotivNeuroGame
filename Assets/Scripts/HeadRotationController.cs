using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;

public class HeadRotationController : MonoBehaviour
{
    [Header("Sensitivity")]
    // How much gyro movement triggers left/right
    [SerializeField] private float threshold    = 15f;
    [SerializeField] private float updateInterval = 0.05f;

    [Header("Smoothing")]
    [SerializeField] private int smoothingWindow = 5;

    // Read by CarController — same as NeuroRacesConnector.CurrentCommand
    public static string CurrentCommand = "neutral";

    private Queue<float> gyroQueue = new Queue<float>();
    private float smoothedGyro = 0f;
    private bool  isRunning    = false;

    void Start()
    {
        // Only activate if head rotation mode was selected in settings
        if (GameSettings.SelectedDirectionMode == GameSettings.DirectionMode.HeadRotation)
        {
            isRunning = true;
            StartCoroutine(ReadGyroLoop());
            Debug.Log("[HeadRotation] Active");
        }
        else
        {
            Debug.Log("[HeadRotation] Not selected — skipping");
        }
    }

    private IEnumerator ReadGyroLoop()
    {
        // Wait for motion stream to start
        yield return new WaitForSeconds(3f);

        while (isRunning)
        {
            // GYROZ = rotation around vertical axis = head turning left/right
            double[] gyroZ = DataStreamManager.Instance.GetMotionData(Channel_t.CHAN_GYROZ);

            float value = 0f;
            if (gyroZ != null && gyroZ.Length > 0)
                value = (float)gyroZ[gyroZ.Length - 1]; // latest sample

            // Smoothing
            gyroQueue.Enqueue(value);
            while (gyroQueue.Count > smoothingWindow)
                gyroQueue.Dequeue();

            float avg = 0f;
            foreach (float v in gyroQueue) avg += v;
            if (gyroQueue.Count > 0) avg /= gyroQueue.Count;
            smoothedGyro = Mathf.Lerp(smoothedGyro, avg, 0.3f);

            // Determine command from head rotation
            if (smoothedGyro > threshold)
                CurrentCommand = "right";
            else if (smoothedGyro < -threshold)
                CurrentCommand = "left";
            else
                CurrentCommand = "neutral";

            Debug.Log($"[HeadRotation] GyroZ: {smoothedGyro:F2} → {CurrentCommand}");

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
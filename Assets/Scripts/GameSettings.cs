using UnityEngine;

// Static class — survives between scenes
public static class GameSettings
{
    // Direction control mode
    public enum DirectionMode { MentalCommands, HeadRotation }
    public static DirectionMode SelectedDirectionMode = DirectionMode.MentalCommands;

    // Speed control mode
    public enum SpeedMode { POW, Keyboard }
    public static SpeedMode SelectedSpeedMode = SpeedMode.POW;

    // Calibration baseline values (set during calibration)
    public static float BaselineAlpha = 1.0f;
    public static float BaselineBeta  = 0.5f;

    // Was headset connected successfully
    public static bool HeadsetConnected = false;
}

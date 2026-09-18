# Neuro Races

A real-time Brain-Computer Interface (BCI) controlled 2D racing game built with **Unity 6** and the **Emotiv EPOC X** EEG headset. The player controls a car using brain signals - no keyboard or mouse required.

Developed as a Bachelor's thesis project at **LAB University of Applied Sciences**, Institute of Design and Fine Art.

---

## Gameplay

The player steers a car through a 5-lane road, avoiding obstacles. The road and obstacles scroll downward automatically - the car stays in place while the world moves around it.

### Control Method

| Control       | Method                                                                 |
| ------------- | ---------------------------------------------------------------------- |
| Road speed    | Beta/Alpha EEG ratio - concentration increases speed                   |
| Car direction | Mental commands (`left` / `right`) or head rotation (magnetometer) |

The more focused the player, the faster the road moves - and the more points they score.

---

## How It Works

```
Emotiv EPOC X
    ↓ Bluetooth
Emotiv Cortex Service (local WebSocket)
    ↓ JSON / WebSocket
Emotiv Unity Plugin
    ↓
NeuroRacesConnector
    ↙              ↘
UpdatePowLoop      UpdateComLoop
(Beta/Alpha ratio) (Mental commands)
    ↓                  ↓
RoadScroller       CarController
(road speed)       (car direction)
    ↓
CSVLogger → Desktop/NeuroRaces/*.csv
```

### EEG Control Logic

**Speed (passive BCI):**

- Reads `pow` data stream from 6 channels: `AF3`, `AF4` (weight 0.5), `F3`, `F4` (weight 0.35), `FC5`, `FC6` (weight 0.15)
- Calculates Beta/Alpha ratio with a 3-step smoothing pipeline
- `targetSpeed = (ratio - threshold) × 4.0`, clamped to `0–6`

**Direction (active BCI):**

- Mental commands via `com` data stream
- Commands accepted only if confidence ≥ `commandPowerThreshold` (default `0.3`)
- Alternative: head rotation via `mot` stream (`CHAN_MAGZ` magnetometer)

---

## Tech Stack

| Tool                | Purpose                        |
| ------------------- | ------------------------------ |
| Unity 6 (C#)        | Game engine                    |
| Emotiv EPOC X       | 14-channel EEG headset, 256 Hz |
| Emotiv Cortex SDK   | WebSocket API for EEG data     |
| Emotiv Unity Plugin | C# wrapper for Cortex SDK      |
| TextMeshPro         | UI text rendering              |
| City Pack           | 2D sprite assets               |

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── AppCredentials.cs        # Single source for Emotiv credentials
│   ├── GameSettings.cs          # Static class — shares settings between scenes
│   ├── NeuroGameConnector.cs    # Main EEG connector (class: NeuroRacesConnector)
│   ├── HeadRotationController.cs# Head rotation via MAGZ magnetometer
│   ├── CarController.cs         # Car movement logic
│   ├── RoadScroller.cs          # Infinite road scrolling (leap-frog)
│   ├── ObstacleSpawner.cs       # Spawns obstacles on random lanes
│   ├── Obstacle.cs              # Obstacle movement behaviour
│   ├── GameManager.cs           # Score, collisions, game over, stats
│   ├── CSVLogger.cs             # Logs EEG + game data to CSV
│   ├── SignalIndicatorUI.cs     # In-game signal indicator (slider -1 to +1)
│   ├── SettingsManager.cs       # Settings scene — connect, calibrate, start
│   ├── MainMenuManager.cs       # Main menu navigation
│   ├── PowSceneManager.cs       # Band Power visualisation scene
│   ├── BandSpectrumReader.cs    # Reads EEG band power for PowScene
│   ├── BandSpectrumGraphUI.cs   # 5-line spectrum graph (LineRenderer)
│   ├── VerticalBandScaleUI.cs   # Vertical activity indicator
├── Emotiv/                      # Emotiv Unity Plugin (SDK)
├── Scenes/
│   ├── MainMenu                 # Main menu
│   ├── SettingScene             # Connect headset + calibration
│   ├── Game                     # Main game scene
│   └── PowScene                 # Real-time EEG visualisation
└── Resources/
    └── City Pack/                # 2D sprite assets
```

---

## Getting Started

### Prerequisites

- Unity 6 (6000.x)
- Emotiv Launcher installed and running
- Emotiv EPOC X headset (or use keyboard test mode)
- Emotiv account with a trained mental commands profile

### Setup

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/NeuroRaces.git
   ```
2. Open the project in Unity 6.
3. Add your Emotiv credentials in `Assets/Scripts/AppCredentials.cs`:

   ```csharp
   public static class AppCredentials
   {
       public const string ClientId = "YOUR_CLIENT_ID";
       public const string ClientSecret = "YOUR_CLIENT_SECRET";
       public const string AppName = "YOUR_APP_NAME";
   }
   ```
4. Set build order in **File → Build Settings**:

   ```
   0: MainMenu
   1: SettingScene
   2: Game
   3: PowScene
   ```
5. Press **Play** in Unity.

---

## How to Play

### With Emotiv EPOC X headset

1. Start Emotiv Launcher and make sure Cortex is running.
2. Open game → Settings.
3. Wait for headset to connect automatically.
4. Choose direction mode: **Mental Commands** or **Head Rotation**.
5. Choose speed mode: **EEG (Band Power)** or **Keyboard**.
6. Complete calibration:
   - 5 seconds - relax, eyes closed
   - 5 seconds - focus actively
7. Press **START** and play!

### Without headset (test mode)

The game automatically falls back to keyboard mode if no headset is detected:

| Key     | Action                     |
| ------- | -------------------------- |
| ← / → | Steer car left / right     |
| Space   | Toggle road movement       |
| ESC     | End session / back to menu |

---

## CSV Data Logging

Every session is automatically saved to `Desktop/NeuroRaces/NeuroRaces_YYYY-MM-DD_HH-mm-ss.csv`.

### Columns

| Column       | Description                                    |
| ------------ | ---------------------------------------------- |
| Time         | Timestamp (`HH:mm:ss.fff`)                   |
| Distance     | Distance driven (meters)                       |
| Collisions   | Total collision count                          |
| Command      | Active mental command (left/right/neutral)     |
| CommandPower | Command confidence (0.000–1.000)              |
| RoadSpeed    | Road scroll speed (0–6)                       |
| IsMoving     | Whether road is moving (True/False)            |
| Alpha_AF3    | Alpha band power, AF3 channel (μV²)          |
| LowBeta_AF3  | Low beta band power, AF3 channel (μV²)       |
| HighBeta_AF3 | High beta band power, AF3 channel (μV²)      |
| Alpha_AF4    | Alpha band power, AF4 channel (μV²)          |
| LowBeta_AF4  | Low beta band power, AF4 channel (μV²)       |
| HighBeta_AF4 | High beta band power, AF4 channel (μV²)      |
| Alpha_F3     | Alpha band power, F3 channel (μV²)           |
| ...          | *(same pattern for F3, F4, FC5, FC6)*        |
| Event        | Special event:`HIT`, `GAME_OVER`, or empty |

---

## Configuration

Key parameters can be adjusted in the Unity Inspector.

### `NeuroRacesConnector`

| Parameter                  | Default   | Description                                 |
| -------------------------- | --------- | ------------------------------------------- |
| `commandPowerThreshold`  | `0.3`   | Minimum confidence to accept mental command |
| `testModeWithoutHeadset` | `true`  | Enable keyboard fallback                    |
| `verboseLogging`         | `false` | Enable detailed console output              |

### `HeadRotationController`

| Parameter           | Default  | Description                                 |
| ------------------- | -------- | ------------------------------------------- |
| `threshold`       | `15.0` | MAGZ deviation to trigger left/right        |
| `deadZone`        | `8.0`  | Zone around center where no command is sent |
| `smoothingWindow` | `5`    | Number of samples for moving average        |

### `GameManager`

| Parameter               | Default | Description                       |
| ----------------------- | ------- | --------------------------------- |
| `gameDurationSeconds` | `300` | Session length in seconds (5 min) |

---

## Known Limitations

- Mental command accuracy depends on training sessions - more training = better accuracy.
- Long hair can reduce electrode contact quality and increase signal noise.
- Magnetometer (MAGZ) drifts after headset placement - recalibrate with the Space key.
- Cortex SDK requires 8–15 seconds to establish connection on startup.

---

## References

- [Emotiv Cortex API](https://emotiv.gitbook.io/cortex-api)
- [Emotiv Unity Plugin](https://github.com/Emotiv/unity-plugin)
- Tarara et al. 2025 - Motor imagery BCI with Emotiv EPOC X
- Ronca et al. 2025 - Real-world benchmarking of wearable EEGs

---

## Author

**Marina Panteleev**
Bachelor's thesis - LAB University of Applied Sciences
Institute of Design and Fine Art
Information and Communication Technology (BEng)
2026

---


## License

This project is licensed under the [MIT License](LICENSE) — see the `LICENSE` file for details.

Note: this covers the original source code only. Third-party components (Emotiv Unity Plugin, Emotiv Cortex SDK, City Pack sprite assets) remain subject to their own respective licenses.

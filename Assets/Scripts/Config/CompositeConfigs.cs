using UnityEngine;

// Composite configurations that group block settings

[System.Serializable]
public class RpiConfig {
    public CameraSettings         camera    = new CameraSettings();
    public TrackerSettings        tracker   = new TrackerSettings();
    public CropSettings           leftCrop  = new CropSettings();
    public CropSettings           rightCrop = new CropSettings();
    public GazeProcessorSettings  gaze      = new GazeProcessorSettings();
}

[System.Serializable]
public class Esp32Config {
    public CameraSettings camera    = new CameraSettings();
    public CropSettings   leftCrop  = new CropSettings();
    public CropSettings   rightCrop = new CropSettings();
}

[System.Serializable]
public class EyeLoopConfig {
    public TrackerSettings        tracker   = new TrackerSettings();
    public CropSettings           leftCrop  = new CropSettings();
    public CropSettings           rightCrop = new CropSettings();
    public GazeProcessorSettings  gaze      = new GazeProcessorSettings();
}

[System.Serializable]
public class TestbedConfig {
    public DisplaySettings displaySettings  = new DisplaySettings();
    public IMUSettings     imuSettings = new IMUSettings();
    public RpiConfig rpi = new RpiConfig();
}

[System.Serializable]
public class UserVRConfig {
    public DisplaySettings displaySettings  = new DisplaySettings();
    public IMUSettings     imuSettings = new IMUSettings();
    public Esp32Config esp = new Esp32Config();
    public EyeLoopConfig   eyeTracker = new EyeLoopConfig();
}
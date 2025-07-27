// Composite configurations that group block settings

[System.Serializable]
public class TestbedConfig
{
    public TrackerSettings tracker = new TrackerSettings();
    public GazeProcessorSettings gaze = new GazeProcessorSettings();
    public CameraSettings camera = new CameraSettings();
    public DisplaySettings displaySettings = new DisplaySettings();
    public IMUSettings imuSettings = new IMUSettings();
    public CropSettings leftCrop = new CropSettings();
    public CropSettings rightCrop = new CropSettings();
}

[System.Serializable]
public class UserVRConfig
{
    public TrackerSettings tracker = new TrackerSettings();
    public GazeProcessorSettings gaze = new GazeProcessorSettings();
    public CameraSettings camera = new CameraSettings();
    public DisplaySettings displaySettings = new DisplaySettings();
    public IMUSettings imuSettings = new IMUSettings();
    public CropSettings leftCrop = new CropSettings();
    public CropSettings rightCrop = new CropSettings();
}
using Contracts;

public static class Settings
{
    public static IConfigProvider<BaseConfig> Provider { private get; set; }

    public static TCPSettings tcp => Provider.Config.tcp;
    public static IMUSettings imu => Provider.Config.imu;
    public static DisplaySettings display => Provider.Config.display;
    public static TrackerSettings tracker => Provider.Config.tracker;
    public static GazeProcessorSettings gaze => Provider.Config.gaze;
    public static CameraSettings camera => Provider.Config.camera;
    public static CropSettings cameraCrop => Provider.Config.cameraCrop;
}

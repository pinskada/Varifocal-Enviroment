using Contracts;

public static class Settings
{
    public static IConfigProvider<BaseConfig> Provider { private get; set; }

    public static TCPSettings TCP => Provider.Config.tcp;
    public static IMUSettings IMU => Provider.Config.imu;
    public static DisplaySettings Display => Provider.Config.display;
    public static TrackerSettings Tracker => Provider.Config.tracker;
    public static GazeProcessorSettings Gaze => Provider.Config.gaze;
    public static CameraSettings Camera => Provider.Config.camera;
    public static CropSettings LeftCrop => Provider.Config.leftCrop;
    public static CropSettings RightCrop => Provider.Config.rightCrop;
}

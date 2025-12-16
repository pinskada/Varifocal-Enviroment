using Contracts;

public static class Settings
{
    public static IConfigProvider<BaseConfig> Provider { private get; set; }

    public static TCPSettings tcp => Provider.Config.tcp;
    public static IMUSettings imu => Provider.Config.imu;
    public static DisplaySettings display => Provider.Config.display;
    public static EyeloopSettings eyeloop => Provider.Config.eyeloop;
    public static GazeProcessorSettings gaze2 => Provider.Config.gaze2;
    public static GazeCalcSettings gazeCalculator => Provider.Config.gazeCalculator;
    public static CameraSettings camera => Provider.Config.camera;
    public static CropSettings tracker_crop => Provider.Config.tracker_crop;
    public static CalibrationSettings calibrationSettings => Provider.Config.calibrationSettings;
}

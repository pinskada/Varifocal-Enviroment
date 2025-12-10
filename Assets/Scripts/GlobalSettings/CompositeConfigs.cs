using System;

// Base configuration shared by all headset versions.
[Serializable]
public abstract class BaseConfig
{
    public TCPSettings tcp = new TCPSettings();
    public TestSettings test = new TestSettings();
    public EyeloopSettings eyeloop = new EyeloopSettings();
    public GazeProcessorSettings gaze = new GazeProcessorSettings();
    public GazeCalcSettings gazeCalculator = new GazeCalcSettings();
    public CameraSettings camera = new CameraSettings();
    public DisplaySettings display = new DisplaySettings();
    public IMUSettings imu = new IMUSettings();
    public CropSettings tracker_crop = new CropSettings();
    public CalibrationSettings calibrationSettings = new CalibrationSettings();
}

// Testbed VR configuration (inherits all from BaseConfig).
[Serializable]
public class TestbedConfig : BaseConfig
{
    // If later you need extra Testbed-specific fields, add them here
}

// User VR configuration (inherits BaseConfig and adds Serial).
[Serializable]
public class UserVRConfig : BaseConfig
{
    public SerialSettings serial = new SerialSettings();
}

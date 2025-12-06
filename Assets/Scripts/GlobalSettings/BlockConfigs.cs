// Block-level data models for settings
using System.Collections.Generic;
using Contracts;

public static class Configuration
{
    public static VRMode currentVersion;
}


[System.Serializable]
public class TCPSettings
{
    public string ipAddress = "192.168.2.1";
    public string raspberryPiIP = "192.168.2.2";
    public string localIP = "127.0.0.1";
    public string subnetMask = "255.255.255.0";
    public string adapterName = "Ethernet";
    public string netshFileName;
    public int port = 65432;
    public int readBufferSize = 16777216; // Size of the buffer for incoming data
    public int IPsetTimeout = 15000; // Timeout in miliseconds for IP configuration
    public int readTimeout = 2000; // Timeout in milliseconds for blocking reads
    public int maxPacketSize = 16777216; // Maximum packet size in bytes (16 MB)
    public int maxSendRetries = 3; // Maximum number of send retries
}

[System.Serializable]
public class SerialSettings
{
    public int baudRate = 115200;
    public int readTimeout = 1000;
    public int writeTimeout = 1000;
}

[System.Serializable]
public class DisplaySettings
{
    public float ipd = 63f; // in mm
    public float screenWidth = 120f; // in mm
    public float screenHeight = 68f; // in mm
    public float eyeToScreenDist = 55f; // in mm
    public float farClipPlane = 1000f; // in meters
    public float distortionStrength = 0.15f; // distortion strength parameter
    public float preZoom = 1f; // overscan pre-scale factor
    public float clampBlack = 0f; // black clamp outside distortion
}


[System.Serializable]
public class IMUSettings
{
    public float minDt = 0.001f; // Minimum delta time for filter updates [s]
    public float maxDt = 0.1f; // Maximum delta time for filter updates [s]
    public float betaMoving = 0.005f; // Madgwick filter beta gain when moving [-]
    public float betaStill = 0.1f; // Madgwick filter beta gain when still [-]
    public float betaThreshold = 0.05f; // Threshold to switch between moving and still states [-]
    public float minGyroMagnitude = 0.01f;  // Threshold to skip updates when gyro is nearly zero [rad/s]
    public float qSmoothAlpha = 0.95f; // Smoothing factor for accelerometer low-pass filter [0..1]

}


[System.Serializable]
public class CameraSettings
{
    public int full_res_width = 2304;
    public int full_res_height = 1296;
    public int focus = 28;
    public int exposure = 10000;
    public int gain = 2;
    public int jpeg_quality = 20;
    public int preview_fps = 5;
}


[System.Serializable]
public class Range { public float min; public float max; }

[System.Serializable]
public class CropRect
{
    public Range x = new Range();
    public Range y = new Range();
}

[System.Serializable]
public class CropSettings
{
    public CropRect crop_left = new CropRect { x = new Range { min = 0f, max = 0.35f }, y = new Range { min = 0.25f, max = 0.6f } };
    public CropRect crop_right = new CropRect { x = new Range { min = 0.65f, max = 1f }, y = new Range { min = 0.25f, max = 0.6f } };
}


[System.Serializable]
public class EyeloopSettings
{
    // Pupil detection settings
    public int left_threshold_pupil = 75;
    public int left_blur_size_pupil = 10;
    public int left_min_radius_pupil = 20;
    public int left_max_radius_pupil = 70;

    public int right_threshold_pupil = 75;
    public int right_blur_size_pupil = 10;
    public int right_min_radius_pupil = 20;
    public int right_max_radius_pupil = 70;


    // Corneal reflection (CR) settings
    public int left_threshold_cr = 160;
    public int left_blur_size_cr = 1;
    public int left_min_radius_cr = 2;
    public int left_max_radius_cr = 10;

    public int right_threshold_cr = 160;
    public int right_blur_size_cr = 1;
    public int right_min_radius_cr = 2;
    public int right_max_radius_cr = 10;
}


[System.Serializable]
public class GazeProcessorSettings
{
    public float filter_alpha = 0.5f;
    public float buffer_crop_factor = 0.1f;
    public float std_threshold = 0.01f;
    public float gyro_threshold = 5f;
}



[System.Serializable]
public class CalibrationSettings
{
    public List<CalibrationPoint> calibrationPoints = new List<CalibrationPoint>
    {
        new CalibrationPoint { id = 1, type = MarkerType.REF, target_position = new TargetPosition { distance = 40f, horizontal = 0.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 2, type = MarkerType.DIST, target_position = new TargetPosition { distance = 8.0f, horizontal = 0.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 3, type = MarkerType.DIST, target_position = new TargetPosition { distance = 4.0f, horizontal = 0.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 4, type = MarkerType.DIST, target_position = new TargetPosition { distance = 2.0f, horizontal = 0.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 5, type = MarkerType.DIST, target_position = new TargetPosition { distance = 1.0f, horizontal = 0.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 6, type = MarkerType.DIST, target_position = new TargetPosition { distance = 0.5f, horizontal = 0.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 7, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = -10.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 8, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = -3.5f, vertical = 0.0f } },
        new CalibrationPoint { id = 9, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = 3.5f, vertical = 0.0f } },
        new CalibrationPoint { id = 10, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = 10.0f, vertical = 0.0f } },
        new CalibrationPoint { id = 11, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = 0.0f, vertical = 10.0f } },
        new CalibrationPoint { id = 12, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = 0.0f, vertical = 3.5f } },
        new CalibrationPoint { id = 13, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = 0.0f, vertical = -3.5f } },
        new CalibrationPoint { id = 14, type = MarkerType.ANG, target_position = new TargetPosition { distance = 2.0f, horizontal = 0.0f, vertical = -10.0f } },
    };
    public float pauseBetweenPoints = 0.5f; // seconds
    public float holdPointTime = 3.0f;   // seconds

}


[System.Serializable]
public class TestSettings
{
    public int testInt = 42;
    public float testFloat = 3.14f;
    public string testString = "TEST1";
}

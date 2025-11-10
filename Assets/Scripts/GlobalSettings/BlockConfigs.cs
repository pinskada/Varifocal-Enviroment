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
    public string adapterName = "Ethernet 2";
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
    public float eyeToScreenDist = 50f; // in mm
    public float farClipPlane = 1000f; // in meters
    public float distortionStrength = 0.15f; // distortion strength parameter
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

}


[System.Serializable]
public class CameraSettings
{
    public int res_width = 1080;
    public int res_height = 720;
    public int focus = 30;
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
    public CropRect crop_left = new CropRect { x = new Range { min = 0f, max = 0.5f }, y = new Range { min = 0f, max = 1f } };
    public CropRect crop_right = new CropRect { x = new Range { min = 0.5f, max = 1f }, y = new Range { min = 0f, max = 1f } };
}


// [System.Serializable]
// public class CropSettings
// {
//     public List<List<float>> crop_left = new List<List<float>>
//     {
//         new List<float> { 0f, 0.5f },   // left, right
//         new List<float> { 0f, 1f }  // top, bottom
//     };

//     public List<List<float>> crop_right = new List<List<float>>
//     {
//         new List<float> { 0.5f, 1f },   // left, right
//         new List<float> { 0f, 1f }  // top, bottom
//     };

// }


[System.Serializable]
public class EyeloopSettings
{
    public int left_threshold = 128;
    public int left_blur_size = 5;
    public int left_min_radius = 5;
    public int left_max_radius = 20;
    public int left_search_step = 10;
    public int right_threshold = 128;
    public int right_blur_size = 5;
    public int right_min_radius = 5;
    public int right_max_radius = 20;
    public int right_search_step = 10;
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
public class TestSettings
{
    public int testInt = 42;
    public float testFloat = 3.14f;
    public string testString = "TEST1";
}

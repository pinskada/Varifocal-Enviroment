// Block-level data models for settings

[System.Serializable]
public class TestSettings
{
    public int testInt = 42;
    public float testFloat = 3.14f;
    public string testString = "TEST1";
}


[System.Serializable]
public class DisplaySettings
{
    public float camIPD = 63f;
    public float dispWidth = 120f;
    public float dispHeight = 68f;
    public float eyeToScreen = 50f;
}


[System.Serializable]
public class IMUSettings
{
    public float BetaMoving = 0.005f; // Madgwick filter beta gain when moving [-]
    public float BetaStill = 0.1f; // Madgwick filter beta gain when still [-]
    public float BetaThreshold = 0.1f; // Threshold to switch between moving and still states [-]
    public float minDt = 0.001f; // Minimum delta time for filter updates [s]
    public float maxDt = 0.1f; // Maximum delta time for filter updates [s]
    public float MinGyroMagnitude = 0.01f;  // Threshold to skip updates when gyro is nearly zero [rad/s]

}


[System.Serializable]
public class CameraSettings {
    public int resWidth    = 1080;
    public int resHeight   = 720;
    public int focus       = 30;
    public int exposure    = 10000;
    public int gain        = 2;
    public int jpegQuality = 20;
    public int previewFps  = 5;
}


[System.Serializable]
public class CropSettings {
    public float left   = 0f;
    public float right  = 0.5f;
    public float top    = 0f;
    public float bottom = 1f;
}


[System.Serializable]
public class TrackerSettings {
    public int minRadius  = 5;
    public int maxRadius  = 20;
    public int searchStep = 10;
}


[System.Serializable]
public class GazeProcessorSettings {
    public float alphaVal          = 0.5f;
    public float bufferCropFactor  = 0.1f;
    public float dataStdThreshold  = 0.01f;
    public float gyroThreshold     = 5f;
}

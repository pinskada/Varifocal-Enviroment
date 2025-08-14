using UnityEngine;
using System.Collections;
using Contracts;

public class IMUHandler : MonoBehaviour, IIMUDataReceiver, IIMUController
{
    // This script handles the IMU data processing and applies the orientation to a target transform in Unity.
    // It uses the Madgwick filter for orientation estimation based on sensor data.

    string moduleName = "IMUHandler"; // Name of the module for configuration
    public IConfigManager _IConfigManager;
    private IOrientationHandler _orientationApplier; // Reference to the orientation applier interface
    public Transform target; // Camera or object to apply the IMU orientation to
    public bool use9DOF = true; // Use 9DOF (gyro, accel, mag) or 6DOF (gyro, accel)
    private Madgwick filter; // Madgwick filter instance for orientation estimation
    private Quaternion initialRotation; // Initial rotation of the target transform to reset to
    private Quaternion q = Quaternion.identity; // Quaternion to hold the current orientation
    private double deltaTime = 0f; // Time since last packet for filter updates
    private double lastPacketTime = 0.0f; // Last packet time for calculating sample period

    // Constants imported from ConfigManager
    public float betaMoving; // Madgwick filter beta gain when moving
    public float betaStill; // Madgwick filter beta gain when still
    public float MinDt; // Minimum delta time for filter updates
    public float MaxDt; // Maximum delta time for filter updates
    public float betaThreshold; // Threshold to switch between moving and still states
    public float minGyroMagnitude; // Threshold to skip updates when gyro is nearly zero


    // Ensure that the sensor data is valid and finite
    static bool IsFinite(double x) => !(double.IsNaN(x) || double.IsInfinity(x));
    static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    static bool IsFinite(Quaternion q) => IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);


    void Start()
    {
        StartCoroutine(WaitForConnection()); // Wait for the orientation applier to be assigned
    }

    void LateUpdate()
    {
        // Ensure the filter and config manager are initialized
        if (filter == null || _IConfigManager == null) return;

        // Check for reset input
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetOrientation();
            return;
        }

        // Update the target rotation based on the filter's quaternion
        q.x = filter.Quaternion[0];
        q.y = filter.Quaternion[1];
        q.z = filter.Quaternion[2];
        q.w = filter.Quaternion[3];

        // Apply the computed orientation to the target transform
        if (_orientationApplier != null)
            _orientationApplier.ApplyOrientation(ConvertSensorToUnity(q));
        else
        {
            Debug.LogWarning("_orientationApplier is not assigned. Cannot apply rotation.");
        }
    }

    private IEnumerator WaitForConnection()
    {
        while (_orientationApplier == null || _IConfigManager == null)
        {
            yield return new WaitForSeconds(0.1f); // Wait until everything is assigned
        }

        _IConfigManager.BindModule(this, moduleName); // Bind this module to the config manager
        initialRotation = _orientationApplier.GetCurrentOrientation(); // Save the starting rotation

        // Initialize the Madgwick filter with the specified sample frequency and beta values
        filter = new Madgwick(betaMoving, betaStill, betaThreshold, minGyroMagnitude);
    }

    public void InjectModules(IOrientationHandler orientationHandler, IConfigManager configManager)
    {
        // Inject the orientation applier to later apply the computed orientation

        _orientationApplier = orientationHandler;
        _IConfigManager = configManager;
    }

    public void UpdateFilter(IMUData imuData)
    {
        // Update the IMU filter with new sensor data

        // Ensure the filter and config manager are initialized
        if (filter == null || _IConfigManager == null) return;

        // Parse the sensor data from the JSON object
        Vector3 gyro = imuData.Gyro * Mathf.Deg2Rad;
        Vector3 accel = imuData.Accel.normalized;
        Vector3 mag = imuData.Mag.normalized;
        double currentTime = imuData.TimeStamp;

        // Drop bad packets early
        if (!IsFinite(currentTime) || !IsFinite(gyro) || !IsFinite(accel) || !IsFinite(mag))
            return;

        // First packet: initialize timeline and bail
        if (lastPacketTime == 0.0f)
        {
            lastPacketTime = currentTime;
            return;
        }

        // Drop late or duplicate packets
        if (currentTime <= lastPacketTime) return;

        // Compute clamped dt
        double rawDt = currentTime - lastPacketTime;
        deltaTime = Mathf.Clamp((float)rawDt, MinDt, MaxDt);
        lastPacketTime = currentTime;

        filter.SetSamplePeriod((float)deltaTime); // Update the filter's sample period

        // Update the filter with the new sensor data
        if (use9DOF)
        {
            // For 9DOF, use gyro, accel, and mag
            filter.Update9DOF(
                gyro.x, gyro.y, gyro.z,
                accel.x, accel.y, accel.z,
                mag.x, mag.y, mag.z
            );
        }
        else
        {
            // For 6DOF, use gyro and accel
            filter.Update6DOF(
                gyro.x, gyro.y, gyro.z,
                accel.x, accel.y, accel.z
            );
        }
    }

    public void ResetOrientation()
    {
        // Make a full reset of the orientation inside the filter and therefore target transform

        if (_orientationApplier != null && initialRotation != null)
        {
            _orientationApplier.ApplyOrientation(initialRotation);

            // Hard reset Madgwick filter quaternion to identity
            filter.Quaternion[0] = 0f;
            filter.Quaternion[1] = 0f;
            filter.Quaternion[2] = 0f;
            filter.Quaternion[3] = 1f;

            Debug.Log("Full reset: camera and filter set to default orientation.");
        }
    }

    private Quaternion ConvertSensorToUnity(Quaternion q)
    {
        // Convert sensor quaternion to Unity's coordinate system

        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}

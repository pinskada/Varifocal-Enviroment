using UnityEngine;
using System.Collections;
using Contracts;
using System.Threading;
using System;
using System.Collections.Concurrent;

public class IMUHandler : MonoBehaviour, IIMUHandler, ModuleSettingsHandler
{
    // This script handles the IMU data processing and applies the orientation to a target transform in Unity.
    // It uses the Madgwick filter for orientation estimation based on sensor data.

    string moduleName = "IMUHandler"; // Name of the module for configuration
    private IConfigManagerConnector _IConfigManager;
    private ICameraHub _ICameraHub; // Reference to the camera hub interface
    private Thread updateThread; // Thread for receiving IMU data
    public Transform target; // Camera or object to apply the IMU orientation to
    //private static readonly ConcurrentQueue<object> IMUqueue = new();
    public bool use9DOF = true; // Use 9DOF (gyro, accel, mag) or 6DOF (gyro, accel)
    private bool isOnline = false; // Flag to control the receive thread
    private Madgwick filter; // Madgwick filter instance for orientation estimation
    private Quaternion initialRotation; // Initial rotation of the target transform to reset to
    private Quaternion q = Quaternion.identity; // Quaternion to hold the current orientation
    private double deltaTime = 0f; // Time since last packet for filter updates
    private double lastPacketTime = 0.0f; // Last packet time for calculating sample period


    //*********************************************************************************************
    // Constants imported from ConfigManager
    public float betaMoving; // Madgwick filter beta gain when moving
    public float BetaMoving
    {
        get => betaMoving;
        set => ChangeFilterSettings(betaMoving, betaStill, betaThreshold, minGyroMagnitude);
    }
    public float betaStill; // Madgwick filter beta gain when still
    public float BetaStill
    {
        get => betaStill;
        set => ChangeFilterSettings(betaMoving, betaStill, betaThreshold, minGyroMagnitude);
    }
    public float betaThreshold; // Threshold to switch between moving and still states
    public float BetaThreshold
    {
        get => betaThreshold;
        set => ChangeFilterSettings(betaMoving, betaStill, betaThreshold, minGyroMagnitude);
    }
    public float minGyroMagnitude; // Threshold to skip updates when gyro is nearly zero
    public float MinGyroMagnitude
    {
        get => minGyroMagnitude;
        set => ChangeFilterSettings(betaMoving, betaStill, betaThreshold, minGyroMagnitude);
    }
    public float minDt; // Minimum delta time for filter updates
    public float maxDt; // Maximum delta time for filter updates
    //*********************************************************************************************


    // Ensure that the sensor data is valid and finite
    static bool IsFinite(double x) => !(double.IsNaN(x) || double.IsInfinity(x));
    static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    static bool IsFinite(Quaternion q) => IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);


    public void InjectModules(ICameraHub cameraHub, IConfigManagerConnector configManager)
    {
        // Inject the external modules interfaces into this handler

        _ICameraHub = cameraHub;
        _IConfigManager = configManager;
    }


    public IEnumerator Start()
    {
        yield return StartCoroutine(WaitForConnection()); // Wait for the orientation applier to be assigned
    }


    private IEnumerator WaitForConnection()
    {
        while (_ICameraHub == null || _IConfigManager == null)
        {
            yield return new WaitForSeconds(0.1f); // Wait until everything is assigned
        }

        _IConfigManager.BindModule(this, moduleName); // Bind this module to the config manager
        initialRotation = _ICameraHub.GetCurrentOrientation(); // Save the starting rotation

        // Initialize the Madgwick filter with the specified sample frequency and beta values
        filter = new Madgwick(betaMoving, betaStill, betaThreshold, minGyroMagnitude);

        Debug.Log("All components and settings initialized.");

        // Start the update thread to listen for incoming messages
        isOnline = true;
        updateThread = new Thread(CheckForData) { IsBackground = true, Name = "IMU.CheckForData" };
        updateThread.Start();

        Debug.Log("IMUHandler thread started.");
    }


    private void CheckForData()
    {
        // This method would handle any periodic updates needed in the thread.
        // Currently, it does nothing but can be expanded if needed.

        while (isOnline)
        {
            while (IMUQueueContainer.IMUqueue.TryDequeue(out var stringIMUdata))
            {
                IMUData imuData = ConvertStringToIMUData(stringIMUdata);
                UpdateFilter(imuData);
            }


            Thread.Sleep(5); // Wait for 0.005 seconds
        }
    }


    public void OnDestroy()
    {
        if (!isOnline) return;
        isOnline = false;

        if (!updateThread.Join(100))
        {
            Debug.LogWarning("IMU.CheckForData thread did not terminate within timeout.");
        }
        updateThread = null;
    }


    private void UpdateFilter(IMUData imuData)
    {
        // Update the IMU filter with new sensor data

        // Ensure the filter and config manager are initialized
        if (filter == null || _IConfigManager == null) return;

        // Parse the sensor data from the JSON object
        Vector3 gyro = imuData.Gyro * Mathf.Deg2Rad;
        Vector3 accel = imuData.Accel.normalized;
        Vector3 mag = imuData.Mag.normalized;
        double tempTime = imuData.TimeStamp;

        // Drop bad packets early
        if (!IsFinite(tempTime) || !IsFinite(gyro) || !IsFinite(accel) || !IsFinite(mag))
            return;

        double currentTime = tempTime;

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
        deltaTime = Mathf.Clamp((float)rawDt, minDt, maxDt);
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


    private void LateUpdate()
    {
        // Ensure the filter and config manager are initialized
        if (filter == null || _IConfigManager == null || _ICameraHub == null) return;

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
        if (_ICameraHub != null)
            _ICameraHub.ApplyOrientation(ConvertSensorToUnity(q));
        else
        {
            Debug.LogWarning("_ICameraHub is not assigned. Cannot apply rotation.");
        }
    }


    public void ResetOrientation()
    {
        // Make a full reset of the orientation inside the filter and therefore target transform

        if (_ICameraHub != null && initialRotation != null)
        {
            _ICameraHub.ApplyOrientation(initialRotation);

            // Hard reset Madgwick filter quaternion to identity
            filter.Quaternion[0] = 0f;
            filter.Quaternion[1] = 0f;
            filter.Quaternion[2] = 0f;
            filter.Quaternion[3] = 1f;

            Debug.Log("Full reset: camera and filter set to default orientation.");
        }
    }


    public void ChangeFilterSettings(float betaMoving, float betaStill, float betaThreshold, float minGyroMagnitude)
    {
        // Change the filter settings dynamically

        this.betaMoving = betaMoving;
        this.betaStill = betaStill;
        this.betaThreshold = betaThreshold;
        this.minGyroMagnitude = minGyroMagnitude;

        if (filter != null)
        {
            filter.SetBetas(betaMoving, betaStill);
            filter.SetBetaThreshold(betaThreshold);
            filter.SetMinGyroMagnitude(minGyroMagnitude);

            Debug.Log("Filter settings updated.");
        }
        else
        {
            Debug.LogWarning("Filter is not initialized. Cannot change settings.");
        }
    }


    private Quaternion ConvertSensorToUnity(Quaternion q)
    {
        // Convert sensor quaternion to Unity's coordinate system

        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }


    // TODO: Implement IMU data conversion
    private IMUData ConvertStringToIMUData(object stringIMUdata)
    {
        if (stringIMUdata == null || !(stringIMUdata is string))
            Debug.LogWarning("Invalid IMU data format.");

        // Convert the incoming action to IMUData and enqueue it for processing
        // Placeholder implementation - replace with actual conversion logic
        IMUData imuData = new IMUData(new Vector3(), new Vector3(), new Vector3(), 0f);

        // Populate the IMUData fields from the action
        return imuData;
    }


    public void ChangeModuleSettings()
    {
        // This method is called when configuration settings change.
        // Currently, it does nothing but can be expanded if needed.
        return;
    }
}

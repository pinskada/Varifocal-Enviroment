using UnityEngine;
using System.Collections;
using Contracts;
using System.Threading;
using System;
using System.Collections.Concurrent;

public class IMUHandler : MonoBehaviour, IIMUHandler, IModuleSettingsHandler
{
    // This script handles the IMU data processing and applies the orientation to a target transform in Unity.
    // It uses the Madgwick filter for orientation estimation based on sensor data.

    private ICameraAligner _ICameraAligner; // Reference to the camera aligner interface
    private Thread updateThread; // Thread for receiving IMU data
    public Transform target; // Camera or object to apply the IMU orientation to
    private Madgwick filter; // Madgwick filter instance for orientation estimation
    private Quaternion initialRotation; // Initial rotation of the target transform to reset to
    private Quaternion q = Quaternion.identity; // Quaternion to hold the current orientation
    public bool use9DOF = true; // Use 9DOF (gyro, accel, mag) or 6DOF (gyro, accel)
    private double deltaTime = 0f; // Time since last packet for filter updates
    private double lastPacketTime = 0.0f; // Last packet time for calculating sample period


    // Ensure that the sensor data is valid and finite
    static bool IsFinite(double x) => !(double.IsNaN(x) || double.IsInfinity(x));
    static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    static bool IsFinite(Quaternion q) => IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);


    public Madgwick GetFilterInstance() => filter;


    public void InjectModules(ICameraAligner cameraAligner)
    {
        // Inject the external modules interfaces into this handler
        _ICameraAligner = cameraAligner;
    }


    private void Start()
    {
        filter = new Madgwick();

        initialRotation = _ICameraAligner.GetCurrentOrientation(); // Save the starting rotation

        // Start the update thread to listen for incoming messages
        updateThread = new Thread(CheckForData) { IsBackground = true, Name = "IMU.CheckForData" };
        updateThread.Start();
    }


    private void CheckForData()
    {
        // This method would handle any periodic updates needed in the thread.
        Debug.Log("[IMUHandler] IMUHandler thread started.");
        foreach (var imuData in IMUQueueContainer.IMUqueue.GetConsumingEnumerable())
        {
            UpdateFilter(imuData);
        }
    }


    public void OnApplicationQuit()
    {
        IMUQueueContainer.IMUqueue.CompleteAdding();

        if (updateThread != null && updateThread.IsAlive)
            updateThread.Join(1000);
    }


    private void UpdateFilter(IMUData imuData)
    {
        // Update the IMU filter with new sensor data

        // Ensure the filter and config manager are initialized
        if (filter == null) return;

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
        deltaTime = Mathf.Clamp((float)rawDt, Settings.IMU.minDt, Settings.IMU.maxDt);
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
        if (filter == null || _ICameraAligner == null) return;

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
        if (_ICameraAligner != null)
            _ICameraAligner.ApplyOrientation(ConvertSensorToUnity(q));
        else
        {
            Debug.LogWarning("[IMUHandler] _ICameraAligner is not assigned. Cannot apply rotation.");
        }
    }


    public void ResetOrientation()
    {
        // Make a full reset of the orientation inside the filter and therefore target transform

        if (_ICameraAligner != null)
        {
            _ICameraAligner.ApplyOrientation(initialRotation);

            // Hard reset Madgwick filter quaternion to identity
            filter.Quaternion[0] = 0f;
            filter.Quaternion[1] = 0f;
            filter.Quaternion[2] = 0f;
            filter.Quaternion[3] = 1f;

            Debug.Log("[IMUHandler] Full reset: camera and filter set to default orientation.");
        }
    }


    private Quaternion ConvertSensorToUnity(Quaternion q)
    {
        // Convert sensor quaternion to Unity's coordinate system

        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }


    public void SettingsChanged(string moduleName, string fieldName)
    {
        // This method is called when configuration settings change.
        // Currently, it does nothing but can be expanded if needed.
        return;
    }
}

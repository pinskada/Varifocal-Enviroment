using UnityEngine;
using Newtonsoft.Json.Linq;
using System;

public class IMUHandler : MonoBehaviour
{
    // This script handles the IMU data processing and applies the orientation to a target transform in Unity.
    // It uses the Madgwick filter for orientation estimation based on sensor data.

    public Transform target; // Camera or object to apply the IMU orientation to
    private float sampleFreq = 100.0f; // Initial refresh rate of the IMU in Hz
    public float betaMoving = 0.005f; // Madgwick filter beta gain
    public float betaStill = 0.1f; // Madgwick filter beta gain when still
    public bool use9DOF = true; // Use 9DOF (gyro, accel, mag) or 6DOF (gyro, accel)
    private Madgwick filter; // Madgwick filter instance for orientation estimation
    private Quaternion initialRotation; // Initial rotation of the target transform to reset to
    private Quaternion q = Quaternion.identity; // Quaternion to hold the current orientation
    private double lastPacketTime = 0f; // Last packet time for calculating sample period
    private double deltaTime = 0f; // Time since last packet for filter updates

    void Start()
    {        
        filter = new Madgwick(1.0f / sampleFreq, betaMoving, betaStill);
        if (target != null)
            initialRotation = target.rotation; // Save the starting rotation
    }

    void Update()
    {
        // Check for reset input
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetOrientation();
            return;
        }
       
        // Update the target rotation based on the filter's quaternion
        q.x = filter.Quaternion[0]; q.y = filter.Quaternion[1]; q.z = filter.Quaternion[2]; q.w = filter.Quaternion[3];
        if (target != null)
            target.rotation = ConvertSensorToUnity(q);
        else
        {
            Debug.LogWarning("[IMUHandler] Target transform is not assigned. Cannot apply rotation.");
        }
    }

    private void ApplySettings()
    {

    }

    public void UpdateFilter(JToken imuData)
    {
        // Update the IMU filter with new sensor data

        // Parse the sensor data from the JSON object
        Vector3 gyro = ParseVector3(imuData["gyro"]) * Mathf.Deg2Rad;
        Vector3 accel = ParseVector3(imuData["accel"]).normalized;
        Vector3 mag = ParseVector3(imuData["mag"]).normalized;

        // Update the sample period based on the time since the last packet
        double currentTime = (double)System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
        if (lastPacketTime != 0f)
        {
            deltaTime = currentTime - lastPacketTime; // Calculate time since last packet
        }
        lastPacketTime = currentTime; // Store the current time for the next update

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
        // Request a full reset of the orientation

        if (target != null)
        {
            target.rotation = initialRotation;

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

    private Vector3 ParseVector3(JToken token)
    {
        // Parse a Vector3 from a JToken, defaulting to (0, 0, 0) if any component is missing

        return new Vector3(
            token["x"]?.ToObject<float>() ?? 0f,
            token["y"]?.ToObject<float>() ?? 0f,
            token["z"]?.ToObject<float>() ?? 0f
        );
    }

}

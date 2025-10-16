using System;
using Mathf = UnityEngine.Mathf;
using UnityEngine;
using Contracts;

public class Madgwick : IModuleSettingsHandler
{
    // This script implements the Madgwick filter for orientation estimation based on IMU data.
    // It supports both 9DOF (gyro, accelerometer, magnetometer) and 6DOF (gyro, accelerometer) modes.

    public float samplePeriod; // Time between updates in seconds
    private float beta; // Current beta value based on motion state
    public float[] Quaternion { get; private set; }// Quaternion representing the orientation (x, y, z, w)


    public Madgwick()
    {
        // Set the beta values for moving and still states
        CheckBetas();
        CheckBetaThreshold();
        CheckMinGyroMagnitude();

        // Initialize quaternion to identity
        Quaternion = new float[] { 0f, 0f, 0f, 1f };
    }


    public void Update9DOF(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz)
    {
        SetBetas(gx, gy, gz);

        // Quaternion components
        float q1 = Quaternion[3], q2 = Quaternion[0], q3 = Quaternion[1], q4 = Quaternion[2];
        float norm;
        float hx, hy, _2bx, _2bz;
        float s1, s2, s3, s4;
        float qDot1, qDot2, qDot3, qDot4;

        // Normalize accelerometer
        norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
        if (norm == 0f) return;
        norm = 1f / norm;
        ax *= norm; ay *= norm; az *= norm;

        // Normalize magnetometer
        norm = (float)Math.Sqrt(mx * mx + my * my + mz * mz);
        if (norm == 0f) return;
        norm = 1f / norm;
        mx *= norm; my *= norm; mz *= norm;

        // Reference direction of Earth's magnetic field
        float _2q1mx = 2f * q1 * mx;
        float _2q1my = 2f * q1 * my;
        float _2q1mz = 2f * q1 * mz;
        float _2q2mx = 2f * q2 * mx;
        float hx_ = mx * q1 * q1 - _2q1my * q4 + _2q1mz * q3 + mx * q2 * q2 + _2q2mx * q3 + mx * q3 * q3 + mx * q4 * q4;
        float hy_ = _2q1mx * q4 + my * q1 * q1 - _2q1mz * q2 + _2q2mx * q3 + my * q3 * q3 - my * q4 * q4;
        hx = (float)Math.Sqrt(hx_ * hx_ + hy_ * hy_);
        hy = 2f * q1 * mx * q3 + mz * q1 * q1 + 2f * q2 * mx * q4 - mz * q2 * q2 + 2f * q3 * my * q4 - mz * q3 * q3 + mz * q4 * q4;
        _2bx = (float)Math.Sqrt(hx * hx + hy * hy);
        _2bz = hy;

        // Gradient descent corrective step
        float _2q1 = 2f * q1;
        float _2q2 = 2f * q2;
        float _2q3 = 2f * q3;
        float _2q4 = 2f * q4;
        float _4q1 = 4f * q1;
        float _4q2 = 4f * q2;
        float _4q3 = 4f * q3;
        float _8q2 = 8f * q2;
        float _8q3 = 8f * q3;

        s1 = _4q1 * q3 * q3 + _2q3 * ax + _4q1 * q2 * q2 - _2q2 * ay + _2bz * q3 * (_2bx * (0.5f - q3 * q3 - q4 * q4) + _2bz * (q2 * q4 - q1 * q3) - mx)
            + _2bz * q4 * (_2bx * (q2 * q3 - q1 * q4) + _2bz * (q1 * q2 + q3 * q4) - my)
            + _2bx * q3 * (_2bx * (q1 * q3 + q2 * q4) + _2bz * (q3 * q4 - q1 * q2) - mz);

        s2 = _4q2 * q4 * q4 - _2q4 * ax + 4f * q1 * q1 * q2 - _2q1 * ay - _4q2 + _8q2 * q2 * q2 + _8q2 * q3 * q3 + _4q2 * az
            + _2bx * q4 * (_2bx * (0.5f - q3 * q3 - q4 * q4) + _2bz * (q2 * q4 - q1 * q3) - mx)
            + (_2bx * q3 + _2bz * q1) * (_2bx * (q2 * q3 - q1 * q4) + _2bz * (q1 * q2 + q3 * q4) - my)
            + (_2bx * q4 - _4q2) * (_2bx * (q1 * q3 + q2 * q4) + _2bz * (q3 * q4 - q1 * q2) - mz);

        s3 = 4f * q1 * q1 * q3 + _2q1 * ax + _4q3 * q4 * q4 - _2q4 * ay - _4q3 + _8q3 * q2 * q2 + _8q3 * q3 * q3 + _4q3 * az
            + (_2bx * q2 - _2bz * q4) * (_2bx * (0.5f - q3 * q3 - q4 * q4) + _2bz * (q2 * q4 - q1 * q3) - mx)
            + (_2bx * q1 + _2bz * q3) * (_2bx * (q2 * q3 - q1 * q4) + _2bz * (q1 * q2 + q3 * q4) - my)
            + (_2bx * q4 - _4q3) * (_2bx * (q1 * q3 + q2 * q4) + _2bz * (q3 * q4 - q1 * q2) - mz);

        s4 = 4f * q2 * q2 * q4 - _2q2 * ax + 4f * q3 * q3 * q4 - _2q3 * ay
            + (_2bx * q3 - _2bz * q2) * (_2bx * (0.5f - q3 * q3 - q4 * q4) + _2bz * (q2 * q4 - q1 * q3) - mx)
            + (_2bx * q4 + _2bz * q1) * (_2bx * (q2 * q3 - q1 * q4) + _2bz * (q1 * q2 + q3 * q4) - my)
            + _2bx * q2 * (_2bx * (q1 * q3 + q2 * q4) + _2bz * (q3 * q4 - q1 * q2) - mz);

        norm = (float)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);
        norm = 1f / norm;
        s1 *= norm; s2 *= norm; s3 *= norm; s4 *= norm;

        qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - beta * s1;
        qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - beta * s2;
        qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - beta * s3;
        qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - beta * s4;

        q1 += qDot1 * samplePeriod;
        q2 += qDot2 * samplePeriod;
        q3 += qDot3 * samplePeriod;
        q4 += qDot4 * samplePeriod;

        if (float.IsNaN(q1) || float.IsNaN(q2) || float.IsNaN(q3) || float.IsNaN(q4))
        {
            Debug.LogWarning("[Madgwick] NaN detected in quaternion update. Skipping frame.");
            return;
        }

        norm = (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
        norm = 1f / norm;
        Quaternion[3] = q1 * norm;
        Quaternion[0] = q2 * norm;
        Quaternion[1] = q3 * norm;
        Quaternion[2] = q4 * norm;
    }


    public void Update6DOF(float gx, float gy, float gz, float ax, float ay, float az)
    {
        SetBetas(gx, gy, gz);

        float q1 = Quaternion[3], q2 = Quaternion[0], q3 = Quaternion[1], q4 = Quaternion[2];
        float norm;
        float s1, s2, s3, s4;
        float qDot1, qDot2, qDot3, qDot4;

        // Normalize accelerometer
        norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
        if (norm == 0f) return;
        norm = 1f / norm;
        ax *= norm; ay *= norm; az *= norm;

        // Gradient descent algorithm corrective step
        float _2q1 = 2f * q1;
        float _2q2 = 2f * q2;
        float _2q3 = 2f * q3;
        float _2q4 = 2f * q4;
        float _4q1 = 4f * q1;
        float _4q2 = 4f * q2;
        float _4q3 = 4f * q3;
        float _8q2 = 8f * q2;
        float _8q3 = 8f * q3;
        float _4q4 = 4f * q4;

        s1 = _4q1 * q3 * q3 + _2q3 * ax + _4q1 * q2 * q2 - _2q2 * ay;
        s2 = _4q2 * q4 * q4 - _2q4 * ax + 4f * q1 * q1 * q2 - _2q1 * ay - _4q2 + _8q2 * q2 * q2 + _8q2 * q3 * q3 + _4q2 * az;
        s3 = 4f * q1 * q1 * q3 + _2q1 * ax + _4q3 * q4 * q4 - _2q4 * ay - _4q3 + _8q3 * q2 * q2 + _8q3 * q3 * q3 + _4q3 * az;
        s4 = 4f * q2 * q2 * q4 - _2q2 * ax + 4f * q3 * q3 * q4 - _2q3 * ay;

        norm = (float)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);
        norm = 1f / norm;
        s1 *= norm; s2 *= norm; s3 *= norm; s4 *= norm;

        qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - beta * s1;
        qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - beta * s2;
        qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - beta * s3;
        qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - beta * s4;

        q1 += qDot1 * samplePeriod;
        q2 += qDot2 * samplePeriod;
        q3 += qDot3 * samplePeriod;
        q4 += qDot4 * samplePeriod;

        if (float.IsNaN(q1) || float.IsNaN(q2) || float.IsNaN(q3) || float.IsNaN(q4))
        {
            Debug.LogWarning("[Madgwick] NaN detected in quaternion update. Skipping frame.");
            return;
        }

        norm = (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
        norm = 1f / norm;
        Quaternion[3] = q1 * norm;
        Quaternion[0] = q2 * norm;
        Quaternion[1] = q3 * norm;
        Quaternion[2] = q4 * norm;
    }


    public void SetSamplePeriod(float samplePeriod)
    {
        // Validate sample period
        if (samplePeriod <= 0f)
        {
            Debug.LogError($"[Madgwick] Sample period must be greater than zero. Current sample period set to {samplePeriod} seconds.");
            return;
        }
        else
        {
            this.samplePeriod = samplePeriod; // Initial time between updates in seconds
        }
    }


    private void SetBetas(float gx, float gy, float gz)
    {
        // Calculate gyro magnitude (radians/sec)
        float gyroMag = Mathf.Sqrt(gx * gx + gy * gy + gz * gz);

        if (gyroMag < Settings.IMU.minGyroMagnitude)
        {
            // If gyro is nearly zero, we can skip the update
            return;
        }

        // Dynamic Beta Adjustment based on motion
        if (gyroMag > Settings.IMU.betaThreshold) // If rotation speed is above 0.1 rad/s (~6 deg/s)
        {
            beta = Settings.IMU.betaMoving; // Moving: trust gyro more, low beta
        }
        else
        {
            beta = Settings.IMU.betaStill; // Still: correct towards accel/mag, higher beta
        }
    }


    public void CheckBetas()
    {
        // Validate beta values
        if (
            Settings.IMU.betaMoving <= 0f ||
            Settings.IMU.betaStill <= 0f ||
            Settings.IMU.betaMoving >= 1f ||
            Settings.IMU.betaStill >= 1f ||
            Settings.IMU.betaMoving > Settings.IMU.betaStill
        )
        {
            Debug.LogError("[Madgwick] Beta values must be greater than zero and betaMoving must be less than betaStill.");
            return;
        }
    }


    public void CheckBetaThreshold()
    {
        // Validate beta threshold
        if (
            Settings.IMU.betaThreshold < Settings.IMU.betaMoving ||
            Settings.IMU.betaThreshold > Settings.IMU.betaStill
        )
        {
            Debug.LogError($"Beta threshold must be between betaMoving and betaStill, but is currently {Settings.IMU.betaThreshold}.");
            return;
        }
    }


    public void CheckMinGyroMagnitude()
    {
        // Validate minimum gyro magnitude
        if (Settings.IMU.minGyroMagnitude < 0f)
        {
            Debug.LogError("[Madgwick] Minimum gyro magnitude must be non-negative.");
            return;
        }
    }


    public void SettingsChanged(string moduleName, string fieldName)
    {
        // This method is called when configuration settings change.
        // Currently, it does nothing but can be expanded if needed.

        if (fieldName == "betaMoving" || fieldName == "betaStill") CheckBetas();
        if (fieldName == "betThreshold") CheckBetaThreshold();
        if (fieldName == "minGyroMagnitude") CheckMinGyroMagnitude();

        return;
    }
}
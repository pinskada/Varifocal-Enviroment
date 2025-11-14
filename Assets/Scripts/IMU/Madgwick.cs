using System;
using UnityEngine;
using Mathf = UnityEngine.Mathf;
using Contracts;

public class Madgwick : IModuleSettingsHandler
{
    // Public state -----------------------------------------------------------

    /// <summary>Time between updates in seconds.</summary>
    public float samplePeriod = 1.0f / 256.0f;

    /// <summary>Quaternion in Unity order: [x, y, z, w].</summary>
    public float[] Quaternion { get; private set; }

    // Internal state in Madgwick order: q0 = w, q1 = x, q2 = y, q3 = z ------
    private float q0 = 1f;
    private float q1 = 0f;
    private float q2 = 0f;
    private float q3 = 0f;

    /// <summary>Current beta (adapted each frame based on motion).</summary>
    private float beta = 0.1f;

    // -----------------------------------------------------------------------

    public Madgwick()
    {
        CheckBetas();
        CheckBetaThreshold();
        CheckMinGyroMagnitude();

        // Identity in Unity order [x,y,z,w]
        Quaternion = new float[] { 0.7f, 0f, 0f, -0.7f };
    }

    // ------------------- Main update functions -----------------------------

    public void Update9DOF(
        float gx, float gy, float gz,
        float ax, float ay, float az,
        float mx, float my, float mz)
    {
        // Adjust beta and possibly zero small gyro values
        SetBetas(gx, gy, gz);

        // If gyro completely zeroed, don't integrate
        if (gx == 0f && gy == 0f && gz == 0f)
            return;

        // If magnetometer invalid, fall back to IMU-only update
        if (mx == 0f && my == 0f && mz == 0f)
        {
            Update6DOF(gx, gy, gz, ax, ay, az);
            return;
        }

        float q0 = this.q0;
        float q1 = this.q1;
        float q2 = this.q2;
        float q3 = this.q3;

        // Rate of change of quaternion from gyroscope
        float qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
        float qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
        float qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
        float qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

        // If accelerometer is valid, apply gradient descent correction
        if (!(ax == 0f && ay == 0f && az == 0f))
        {
            // Normalize accelerometer
            float recipNorm = 1.0f / Mathf.Sqrt(ax * ax + ay * ay + az * az);
            ax *= recipNorm;
            ay *= recipNorm;
            az *= recipNorm;

            // Normalize magnetometer
            recipNorm = 1.0f / Mathf.Sqrt(mx * mx + my * my + mz * mz);
            mx *= recipNorm;
            my *= recipNorm;
            mz *= recipNorm;

            // Auxiliary variables to avoid repeated arithmetic
            float _2q0mx = 2.0f * q0 * mx;
            float _2q0my = 2.0f * q0 * my;
            float _2q0mz = 2.0f * q0 * mz;
            float _2q1mx = 2.0f * q1 * mx;
            float _2q0 = 2.0f * q0;
            float _2q1 = 2.0f * q1;
            float _2q2 = 2.0f * q2;
            float _2q3 = 2.0f * q3;
            float _2q0q2 = 2.0f * q0 * q2;
            float _2q2q3 = 2.0f * q2 * q3;
            float q0q0 = q0 * q0;
            float q0q1 = q0 * q1;
            float q0q2 = q0 * q2;
            float q0q3 = q0 * q3;
            float q1q1 = q1 * q1;
            float q1q2 = q1 * q2;
            float q1q3 = q1 * q3;
            float q2q2 = q2 * q2;
            float q2q3 = q2 * q3;
            float q3q3 = q3 * q3;

            // Reference direction of Earth's magnetic field
            float hx = mx * q0q0 - _2q0my * q3 + _2q0mz * q2 + mx * q1q1 + _2q1 * my * q2 + _2q1 * mz * q3 - mx * q2q2 - mx * q3q3;
            float hy = _2q0mx * q3 + my * q0q0 - _2q0mz * q1 + _2q1mx * q2 - my * q1q1 + my * q2q2 + _2q2 * mz * q3 - my * q3q3;
            float _2bx = Mathf.Sqrt(hx * hx + hy * hy);
            float _2bz = -_2q0mx * q2 + _2q0my * q1 + mz * q0q0 + _2q1mx * q3 - mz * q1q1 + _2q2 * my * q3 - mz * q2q2 + mz * q3q3;
            float _4bx = 2.0f * _2bx;
            float _4bz = 2.0f * _2bz;

            // Gradient descent algorithm corrective step
            float s0 = -_2q2 * (2.0f * (q1q3 - q0q2) - ax)
                       + _2q1 * (2.0f * (q0q1 + q2q3) - ay)
                       - _2bz * q2 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx)
                       + (-_2bx * q3 + _2bz * q1) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my)
                       + _2bx * q2 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);

            float s1 = _2q3 * (2.0f * (q1q3 - q0q2) - ax)
                       + _2q0 * (2.0f * (q0q1 + q2q3) - ay)
                       - 4.0f * q1 * (2.0f * (0.5f - q1q1 - q2q2) - az)
                       + _2bz * q3 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx)
                       + (_2bx * q2 + _2bz * q0) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my)
                       + (_2bx * q3 - _4bz * q1) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);

            float s2 = -_2q0 * (2.0f * (q1q3 - q0q2) - ax)
                       + _2q3 * (2.0f * (q0q1 + q2q3) - ay)
                       - 4.0f * q2 * (2.0f * (0.5f - q1q1 - q2q2) - az)
                       + (-_4bx * q2 - _2bz * q0) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx)
                       + (_2bx * q1 + _2bz * q3) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my)
                       + (_2bx * q0 - _4bz * q2) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);

            float s3 = _2q1 * (2.0f * (q1q3 - q0q2) - ax)
                       + _2q2 * (2.0f * (q0q1 + q2q3) - ay)
                       + (-_4bx * q3 + _2bz * q1) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx)
                       + (-_2bx * q0 + _2bz * q2) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my)
                       + _2bx * q1 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);

            // Normalize step magnitude
            recipNorm = 1.0f / Mathf.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3);
            s0 *= recipNorm;
            s1 *= recipNorm;
            s2 *= recipNorm;
            s3 *= recipNorm;

            // Apply feedback
            qDot1 -= beta * s0;
            qDot2 -= beta * s1;
            qDot3 -= beta * s2;
            qDot4 -= beta * s3;
        }

        // Integrate to yield quaternion
        q0 += qDot1 * samplePeriod;
        q1 += qDot2 * samplePeriod;
        q2 += qDot3 * samplePeriod;
        q3 += qDot4 * samplePeriod;

        // Normalize quaternion
        float normQ = 1.0f / Mathf.Sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
        q0 *= normQ;
        q1 *= normQ;
        q2 *= normQ;
        q3 *= normQ;

        // Store back
        this.q0 = q0;
        this.q1 = q1;
        this.q2 = q2;
        this.q3 = q3;

        // Update public Unity-order quaternion [x,y,z,w]
        Quaternion[0] = q1;
        Quaternion[1] = q2;
        Quaternion[2] = q3;
        Quaternion[3] = q0;
    }


    public void Update6DOF(float gx, float gy, float gz,
                           float ax, float ay, float az)
    {
        SetBetas(gx, gy, gz);

        // If gyro completely zeroed, don't integrate
        if (gx == 0f && gy == 0f && gz == 0f)
            return;

        float q0 = this.q0;
        float q1 = this.q1;
        float q2 = this.q2;
        float q3 = this.q3;

        // Rate of change of quaternion from gyroscope
        float qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
        float qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
        float qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
        float qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

        // If accelerometer is valid, apply gradient descent correction
        if (!(ax == 0f && ay == 0f && az == 0f))
        {
            // Normalize accelerometer
            float recipNorm = 1.0f / Mathf.Sqrt(ax * ax + ay * ay + az * az);
            ax *= recipNorm;
            ay *= recipNorm;
            az *= recipNorm;

            // Gradient descent algorithm corrective step
            float _2q0 = 2.0f * q0;
            float _2q1 = 2.0f * q1;
            float _2q2 = 2.0f * q2;
            float _2q3 = 2.0f * q3;
            float _4q0 = 4.0f * q0;
            float _4q1 = 4.0f * q1;
            float _4q2 = 4.0f * q2;
            float _8q1 = 8.0f * q1;
            float _8q2 = 8.0f * q2;
            float q0q0 = q0 * q0;
            float q1q1 = q1 * q1;
            float q2q2 = q2 * q2;
            float q3q3 = q3 * q3;

            float s0 = _4q0 * q2q2 + _2q2 * ax
                       + _4q0 * q1q1 - _2q1 * ay;
            float s1 = _4q1 * q3q3 - _2q3 * ax
                       + 4.0f * q0q0 * q1 - _2q0 * ay
                       - _4q1 + _8q1 * q1q1 + _8q1 * q2q2
                       + _4q1 * az;
            float s2 = 4.0f * q0q0 * q2 + _2q0 * ax
                       + _4q2 * q3q3 - _2q3 * ay
                       - _4q2 + _8q2 * q1q1 + _8q2 * q2q2
                       + _4q2 * az;
            float s3 = 4.0f * q1q1 * q3 - _2q1 * ax
                       + 4.0f * q2q2 * q3 - _2q2 * ay;

            // Normalize step magnitude
            recipNorm = 1.0f / Mathf.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3);
            s0 *= recipNorm;
            s1 *= recipNorm;
            s2 *= recipNorm;
            s3 *= recipNorm;

            // Apply feedback
            qDot1 -= beta * s0;
            qDot2 -= beta * s1;
            qDot3 -= beta * s2;
            qDot4 -= beta * s3;
        }

        // Integrate to yield quaternion
        q0 += qDot1 * samplePeriod;
        q1 += qDot2 * samplePeriod;
        q2 += qDot3 * samplePeriod;
        q3 += qDot4 * samplePeriod;

        // Normalize quaternion
        float normQ = 1.0f / Mathf.Sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
        q0 *= normQ;
        q1 *= normQ;
        q2 *= normQ;
        q3 *= normQ;

        // Store back
        this.q0 = q0;
        this.q1 = q1;
        this.q2 = q2;
        this.q3 = q3;

        // Update public Unity-order quaternion [x,y,z,w]
        Quaternion[0] = q1;
        Quaternion[1] = q2;
        Quaternion[2] = q3;
        Quaternion[3] = q0;
    }

    // ------------------- Utility / config methods -------------------------

    public void SetSamplePeriod(float samplePeriod)
    {
        if (samplePeriod <= 0f)
        {
            Debug.LogError($"[Madgwick] Sample period must be > 0. Provided: {samplePeriod}");
            return;
        }
        this.samplePeriod = samplePeriod;
    }

    /// <summary>
    /// Sets beta based on current gyro magnitude and optionally zeros very small gyro.
    /// Returns possibly modified (gx, gy, gz).
    /// </summary>
    private (float, float, float) SetBetas(float gx, float gy, float gz)
    {
        float gyroMag = Mathf.Sqrt(gx * gx + gy * gy + gz * gz);

        // Dynamic beta based on motion
        if (gyroMag > Settings.imu.betaThreshold)
        {
            //Debug.Log("Beta set to moving");
            beta = Settings.imu.betaMoving;  // fast motion
        }
        else
        {
            //Debug.Log("Beta set to still");
            beta = Settings.imu.betaStill;   // still / slow: stronger accel/mag correction
        }

        // If gyro almost zero, zero it out completely so there is no integration
        if (gyroMag < Settings.imu.minGyroMagnitude)
        {
            return (0f, 0f, 0f);
        }

        return (gx, gy, gz);
    }

    // Validation -----------------------------------------------------------

    public void CheckBetas()
    {
        if (Settings.imu.betaMoving < 0f ||
            Settings.imu.betaStill < 0f ||
            Settings.imu.betaMoving > 1f ||
            Settings.imu.betaStill > 1f)// ||
                                        //Settings.imu.betaMoving > Settings.imu.betaStill)
        {
            Debug.LogError("[Madgwick] betaMoving/betaStill must be in (0,1) and betaMoving < betaStill.");
        }
    }

    public void CheckBetaThreshold()
    {
        if (Settings.imu.betaThreshold < 0f || float.IsNaN(Settings.imu.betaThreshold))
        {
            Debug.LogError("[Madgwick] betaThreshold must be >= 0 (rad/s).");
        }
    }

    public void CheckMinGyroMagnitude()
    {
        if (Settings.imu.minGyroMagnitude < 0f)
        {
            Debug.LogError("[Madgwick] minGyroMagnitude must be >= 0.");
        }
    }

    public void SettingsChanged(string moduleName, string fieldName)
    {
        if (fieldName == "betaMoving" || fieldName == "betaStill") CheckBetas();
        if (fieldName == "betaThreshold") CheckBetaThreshold();     // fixed typo
        if (fieldName == "minGyroMagnitude") CheckMinGyroMagnitude();
    }
}

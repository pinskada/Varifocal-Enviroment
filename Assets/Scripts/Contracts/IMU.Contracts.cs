using UnityEngine;

namespace Contracts {
    // Data Transfer Object carrying raw IMU readings (gyro, accel, mag) and timing info.
    public class IMUData {
        // Gyroscope readings in radians/sec.
        public Vector3 Gyro { get; }
        // Accelerometer readings in g.
        public Vector3 Accel { get; }
        // Magnetometer readings in µT.
        public Vector3 Mag { get; }
        // Time since last sample in seconds (measured at the data source).
        public float DeltaTime { get; }

        public IMUData(Vector3 gyro, Vector3 accel, Vector3 mag, float deltaTime) {
            Gyro = gyro;
            Accel = accel;
            Mag = mag;
            DeltaTime = deltaTime;
        }
    }

    // Contract for consuming IMU data streams by updating the internal filter.
    public interface IIMUDataReceiver {
        // Called each time a new IMU sample is available to process.
        void UpdateFilter(IMUData data);
    }

    // Contract for sending commands or real-time parameters to the IMU module.
    public interface IIMUController
    {
        // Reset the IMU’s orientation state.
        void ResetOrientation();
    }

    // Contract for applying orientation updates from IMU to any target (e.g., camera, network stream).
    public interface IOrientationHandler
    {
        // Apply a world-space rotation quaternion.
        void ApplyOrientation(Quaternion worldRotation);
        
        Quaternion GetCurrentOrientation();
    }
}

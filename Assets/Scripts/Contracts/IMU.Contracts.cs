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
        public float TimeStamp { get; }

        public IMUData(Vector3 gyro, Vector3 accel, Vector3 mag, float timeStamp) {
            Gyro = gyro;
            Accel = accel;
            Mag = mag;
            TimeStamp = timeStamp;
        }
    }

    // Contract for consuming data or controls by the IMUHandler.
    public interface IIMUHandler
    {
        // Called each time a new IMU sample is available to process.
        void UpdateFilter(IMUData data);

        // Reset the IMU’s orientation state.
        void ResetOrientation();
    }
}

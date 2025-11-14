using UnityEngine;
using System.Collections.Concurrent;

namespace Contracts
{
    // Data Transfer Object carrying raw IMU readings (gyro, accel, mag) and timing info.
    [System.Serializable]
    public class IMUData
    {
        // Gyroscope readings in radians/sec.
        public Vector3 gyro;
        // Accelerometer readings in g.
        public Vector3 accel;
        // Magnetometer readings in µT.
        public Vector3 mag;
        // Time since last sample in seconds (measured at the data source).
        public double timestamp;
    }

    // Container for the IMU data queue.
    public static class IMUQueueContainer
    {
        public static readonly BlockingCollection<IMUData> IMUqueue = new BlockingCollection<IMUData>();
    }

    // Contract for consuming data or controls by the IMUHandler.
    public interface IIMUHandler
    {
        // Reset the IMU’s orientation state.
        void ResetOrientation();
    }
}

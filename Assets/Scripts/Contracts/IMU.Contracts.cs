using UnityEngine;
using System.Collections.Concurrent;

namespace Contracts
{
    // Data Transfer Object carrying raw IMU readings (gyro, accel, mag) and timing info.
    public class IMUData
    {
        // Gyroscope readings in radians/sec.
        public Vector3 Gyro { get; }
        // Accelerometer readings in g.
        public Vector3 Accel { get; }
        // Magnetometer readings in µT.
        public Vector3 Mag { get; }
        // Time since last sample in seconds (measured at the data source).
        public float TimeStamp { get; }

        public IMUData(Vector3 gyro, Vector3 accel, Vector3 mag, float timeStamp)
        {
            Gyro = gyro;
            Accel = accel;
            Mag = mag;
            TimeStamp = timeStamp;
        }
    }

    // Container for the IMU data queue.
    public static class IMUQueueContainer
    {
        public static readonly ConcurrentQueue<IMUData> IMUqueue = new ConcurrentQueue<IMUData>();
    }

    // Contract for consuming data or controls by the IMUHandler.
    public interface IIMUHandler
    {
        // Reset the IMU’s orientation state.
        void ResetOrientation();
    }
}

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

    // Contract for consuming IMU data streams.
    public interface IIMUWriter
    {
        // Called each time a new IMU sample or batch is available.
        void OnIMUDataUpdate(IMUData data);

        void ResetOrientation();
    }
}

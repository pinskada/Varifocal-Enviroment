using UnityEngine;

namespace Contracts
{
    public enum MessageType {
        EyeImage,
        IMU,
        GazeDistance,
        VarifocalControl
    }

    public enum TransportType {
        Tcp,
        Serial
    }
}
using UnityEngine;

namespace Contracts
{
    public enum MessageType
    {
        imu,
        unityControl,
        gazeDistance,
        tcpLogg,
        tcpConfig,
        tcpControl,
        espLogg,
        espConfig,
        espControl,
        trackerPreview,
        eyePreview,
        eyeImage,
    }

    public enum FormatType
    {
        JSON,
        PNG,
        JPEG
    }

    public enum TransportSource
    {
        Tcp,
        Serial,
        Unity
    }

    public enum TransportTarget
    {
        Tcp,
        Serial,
        Unity
    }

    public enum EncodingType
    {
        Encode,
        Decode,
        None
    }
}

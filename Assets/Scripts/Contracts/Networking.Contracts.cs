using System.Collections.Concurrent;

namespace Contracts
{
    public enum MessageType
    {
        imu = 0,
        unityControl = 1,
        gazeDistance = 2,
        generalConfig = 3,
        tcpLogg = 4,
        tcpConfig = 5,
        tcpControl = 6,
        espLogg = 7,
        espConfig = 8,
        espControl = 9,
        trackerPreview = 10,
        eyePreview = 11,
        eyeImage = 12
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
    public static class RouteQueueContainer
    {
        public static readonly BlockingCollection<(object payload, MessageType messageType)> routeQueue =
            new BlockingCollection<(object payload, MessageType messageType)>();
    }
}

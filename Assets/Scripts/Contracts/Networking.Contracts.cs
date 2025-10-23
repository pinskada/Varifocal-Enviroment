using System.Collections.Concurrent;

namespace Contracts
{
    public enum MessageType
    {
        imuSensor = 0,
        imuFilter = 1,
        gazeData = 2,
        gazeCalcControl = 3,
        gazeSceneControl = 4,
        trackerControl = 5,
        tcpConfig = 6,
        espConfig = 7,
        tcpLogg = 8,
        espLogg = 9,
        trackerPreview = 10,
        eyePreview = 11,
        eyeImage = 12,
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

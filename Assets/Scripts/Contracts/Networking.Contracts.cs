using UnityEngine;
using System.Collections.Concurrent;

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
    public static class RouteQueueContainer
    {
        public static readonly ConcurrentQueue<(object payload, MessageType messageType)> routeQueue =
            new ConcurrentQueue<(object payload, MessageType messageType)>();
    }
}

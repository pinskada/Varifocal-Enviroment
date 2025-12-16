using System;
using System.Collections.Concurrent;

namespace Contracts
{
    public enum MessageType
    {
        imuSensor = 0,
        imuCmd = 1,
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
        configReady = 13,
        trackerData = 14,
        ipdPreview = 15,
        sceneMarker = 16,
        calibData = 17,
        eyeVectors = 18
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

    public struct EyeImage
    {
        public int EyeId;     // 0 = left, 1 = right
        public int Width;
        public int Height;
        public byte[] Data;   // compressed image bytes
    }

    public static class CommEvents
    {
        public static event Action TcpConnected;
        public static event Action TcpDisconnected;

        public static void RaiseTcpConnected() => TcpConnected?.Invoke();
        public static void RaiseTcpDisconnected() => TcpDisconnected?.Invoke();
    }
}

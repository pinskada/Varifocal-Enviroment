using Contracts;
using System.Collections.Generic;
using System;
using UnityEngine;

public static class RoutingTable
{
    // Defines hardcoded routing profiles.

    public static Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)> CreateGlobalRoutingTable()
    {
        // Create the global routing table based on the current VRMode.
        // It is a table of how to route each message type across possible transports.
        // Each entry maps a MessageType to a tuple of (TransportSource, TransportTarget, FormatType).

        var routingTable = new Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)>();

        switch (Configuration.currentVersion)
        {
            case VRMode.Testbed:
                routingTable[MessageType.imu] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.unityControl] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.gazeDistance] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.tcpLogg] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.tcpConfig] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.tcpControl] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.trackerPreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.PNG);
                routingTable[MessageType.eyePreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JPEG);
                break;

            case VRMode.UserVR:
                routingTable[MessageType.imu] = (TransportSource.Serial, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.unityControl] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.gazeDistance] = (TransportSource.Tcp, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.tcpLogg] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.tcpConfig] = (TransportSource.Unity, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.tcpControl] = (TransportSource.Unity, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.espLogg] = (TransportSource.Serial, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.espConfig] = (TransportSource.Unity, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.espControl] = (TransportSource.Unity, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.trackerPreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.PNG);
                routingTable[MessageType.eyePreview] = (TransportSource.Serial, TransportTarget.Unity, FormatType.JPEG);
                routingTable[MessageType.eyeImage] = (TransportSource.Serial, TransportTarget.Serial, FormatType.JPEG);
                break;

            default:
                Debug.LogError($"CommRouter: Unsupported VRMode {Configuration.currentVersion}");
                break;
        }

        return routingTable;
    }

    public static Dictionary<MessageType, Action<object>> CreateLocalRoutingTable()
    {
        // Create the local routing table.
        // It is a table of how to handle each message type within Unity.

        var localRoutingTable = new Dictionary<MessageType, Action<object>>();

        localRoutingTable[MessageType.imu] = (payload) => HandleIMUData(payload);
        localRoutingTable[MessageType.tcpLogg] = (payload) => Debug.Log($"TCP Log: {payload}");
        localRoutingTable[MessageType.espLogg] = (payload) => Debug.Log($"ESP Log: {payload}");
        localRoutingTable[MessageType.trackerPreview] = (texture2D) => GUIQueueContainer.GUIqueue.Enqueue((Texture2D)texture2D);
        localRoutingTable[MessageType.eyePreview] = (texture2D) => GUIQueueContainer.GUIqueue.Enqueue((Texture2D)texture2D);

        return localRoutingTable;
    }

    public static List<RoutingEntry> CreateTCPModuleRoutingList()
    {
        // Create the list of routing entries for the TCP module based on the current VRMode.
        // Each entry contains a name (for logging) and a getter function to access the relevant settings object.

        var tcpRoutingList = new List<RoutingEntry>();

        switch (Configuration.currentVersion)
        {
            case VRMode.Testbed:
                tcpRoutingList.Add(new RoutingEntry("camera", () => Settings.Camera));
                tcpRoutingList.Add(new RoutingEntry("rightCrop", () => Settings.RightCrop));
                tcpRoutingList.Add(new RoutingEntry("leftCrop", () => Settings.LeftCrop));
                tcpRoutingList.Add(new RoutingEntry("tracker", () => Settings.Tracker));
                tcpRoutingList.Add(new RoutingEntry("gaze", () => Settings.Gaze));
                break;

            case VRMode.UserVR:
                tcpRoutingList.Add(new RoutingEntry("tracker", () => Settings.Tracker));
                break;
        }

        return tcpRoutingList;
    }

    public static List<RoutingEntry> CreateSerialModuleRoutingList()
    {
        // Create the list of routing entries for the Serial module based on the current VRMode.
        // Each entry contains a name (for logging) and a getter function to access the relevant settings object.

        var serialRoutingList = new List<RoutingEntry>();

        switch (Configuration.currentVersion)
        {
            case VRMode.UserVR:
                serialRoutingList.Add(new RoutingEntry("camera", () => Settings.Camera));
                serialRoutingList.Add(new RoutingEntry("rightCrop", () => Settings.RightCrop));
                serialRoutingList.Add(new RoutingEntry("leftCrop", () => Settings.LeftCrop));
                serialRoutingList.Add(new RoutingEntry("gaze", () => Settings.Gaze));
                break;
        }

        return serialRoutingList;
    }

    private static void HandleIMUData(object payload)
    {
        // Parse and enqueue IMU data.

        IMUData imuData;

        try
        {
            imuData = JsonUtility.FromJson<IMUData>(payload.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse IMU data: {ex.Message}");
            return;
        }
        IMUQueueContainer.IMUqueue.Enqueue(imuData);
    }
}

public class RoutingEntry
{
    // Creates a routing entry with a name and a getter function for getting settings.

    // Name is optional (for logging/debugging)
    public string Name { get; }

    // Accessor to the actual settings object (from StaticWrapper)
    public Func<object> GetSettings { get; }

    public RoutingEntry(string name, Func<object> getter)
    {
        Name = name;
        GetSettings = getter;
    }
}

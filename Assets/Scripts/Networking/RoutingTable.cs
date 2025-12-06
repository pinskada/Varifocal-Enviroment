using Contracts;
using System.Collections.Generic;
using System;
using UnityEngine;


public static class RoutingTable
{
    // Defines hardcoded routing profiles.

    private static ImageDecoder imageDecoder;

    public static Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)> CreateGlobalRoutingTable()
    {
        // Create the global routing table based on the current VRMode.
        // It is a table of how to route each message type across possible transports.
        // Each entry maps a MessageType to a tuple of (TransportSource, TransportTarget, FormatType).

        var routingTable = new Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)>();

        switch (Configuration.currentVersion)
        {
            case VRMode.Testbed:
                routingTable[MessageType.imuCmd] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.imuSensor] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.gazeData] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.gazeCalcControl] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.gazeSceneControl] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.trackerControl] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.tcpConfig] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.espConfig] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.tcpLogg] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.espLogg] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.trackerPreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.PNG);
                routingTable[MessageType.eyePreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JPEG);
                routingTable[MessageType.eyeImage] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.configReady] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.trackerData] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.ipdPreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.sceneMarker] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.calibData] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                break;

            case VRMode.UserVR:
                routingTable[MessageType.imuSensor] = (TransportSource.Unity, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.imuCmd] = (TransportSource.Serial, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.gazeData] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.gazeCalcControl] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.gazeSceneControl] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.trackerControl] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.tcpConfig] = (TransportSource.Unity, TransportTarget.Tcp, FormatType.JSON);
                routingTable[MessageType.espConfig] = (TransportSource.Unity, TransportTarget.Serial, FormatType.JSON);
                routingTable[MessageType.tcpLogg] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.espLogg] = (TransportSource.Serial, TransportTarget.Unity, FormatType.JSON);
                routingTable[MessageType.trackerPreview] = (TransportSource.Tcp, TransportTarget.Unity, FormatType.PNG);
                routingTable[MessageType.eyePreview] = (TransportSource.Serial, TransportTarget.Unity, FormatType.JPEG);
                routingTable[MessageType.eyeImage] = (TransportSource.Serial, TransportTarget.Serial, FormatType.JPEG);
                break;

            default:
                Debug.LogError($"[CommRouter] Unsupported VRMode {Configuration.currentVersion}");
                break;
        }

        return routingTable;
    }


    public static Dictionary<MessageType, Action<object>> CreateLocalRoutingTable()
    {
        // Create the local routing table.
        // It is a table of how to handle each message type within Unity.

        var localRoutingTable = new Dictionary<MessageType, Action<object>>();

        localRoutingTable[MessageType.imuSensor] = (payload) => HandleIMUData(payload);
        localRoutingTable[MessageType.tcpLogg] = (payload) => Debug.Log($"TCP Log: {payload}");
        localRoutingTable[MessageType.espLogg] = (payload) => Debug.Log($"ESP Log: {payload}");
        localRoutingTable[MessageType.trackerPreview] = (payload) => HandlePreviewImage(payload);
        localRoutingTable[MessageType.eyePreview] = (payload) => HandlePreviewImage(payload);
        localRoutingTable[MessageType.trackerData] = (payload) => HandleTrackerData(payload);
        localRoutingTable[MessageType.gazeData] = (payload) => Debug.Log($"Gaze Data Received: {payload}");
        localRoutingTable[MessageType.calibData] = (payload) => Debug.Log($"Calibration Data Received: {payload}");

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
                tcpRoutingList.Add(new RoutingEntry("camera", () => Settings.camera));
                tcpRoutingList.Add(new RoutingEntry("tracker_crop", () => Settings.tracker_crop));
                tcpRoutingList.Add(new RoutingEntry("eyeloop", () => Settings.eyeloop));
                tcpRoutingList.Add(new RoutingEntry("gaze", () => Settings.gaze));
                break;

            case VRMode.UserVR:
                tcpRoutingList.Add(new RoutingEntry("eyeloop", () => Settings.eyeloop));
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
            case VRMode.Testbed: // Serial not used in Testbed mode
                break;
            case VRMode.UserVR:
                serialRoutingList.Add(new RoutingEntry("camera", () => Settings.camera));
                serialRoutingList.Add(new RoutingEntry("tracker_crop", () => Settings.tracker_crop));
                serialRoutingList.Add(new RoutingEntry("gaze", () => Settings.gaze));
                break;
        }

        return serialRoutingList;
    }


    private static void HandleTrackerData(object payload)
    {
        //Debug.Log("[CommRouter] Handling tracker data.");
        // Handle tracker data.
        if (payload == null)
        {
            Debug.LogError("[CommRouter] HandleTrackerData received null payload.");
            return;
        }

        var json = payload as string;
        if (json == null)
        {
            Debug.LogError($"[CommRouter] HandleTrackerData expected JSON string, got {payload.GetType().Name}.");
            return;
        }

        try
        {
            TrackerData trackerData = JsonUtility.FromJson<TrackerData>(json);
            GUIQueueContainer.trackerData.Enqueue(trackerData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CommRouter] Failed to parse tracker data: {ex.Message}");
            return;
        }
    }

    private static void HandleIMUData(object payload)
    {
        // Parse and enqueue IMU data.

        if (payload == null)
        {
            Debug.LogError("[CommRouter] HandleIMUData received null payload.");
            return;
        }

        var json = payload as string;
        if (json == null)
        {
            Debug.LogError($"[CommRouter] HandleIMUData expected JSON string, got {payload.GetType().Name}.");
            return;
        }

        try
        {
            IMUData imuData = JsonUtility.FromJson<IMUData>(json);
            IMUQueueContainer.IMUqueue.Add(imuData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CommRouter] Failed to parse IMU data: {ex.Message}");
            return;
        }
    }


    private static void HandlePreviewImage(object payload)
    {
        // Handle preview image data.
        // This function can be expanded to process the image as needed.
        var images = payload as List<EyeImage>;
        if (images == null)
        {
            Debug.LogError("[CommRouter] HandlePreviewImage: Payload is not a list of EyeData.");
            return;
        }

        GUIQueueContainer.images.Enqueue(images);
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

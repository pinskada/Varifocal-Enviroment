using Contracts;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mono.Cecil.Cil;

public static class RoutingTable
{
    // Defines hardcoded routing profiles for each mode.

    public static Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)> CreateRoutingTable()
    {
        var routingTable = new Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)>();
        // Initialize the routing table based on the active VR mode.
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
        var localRoutingTable = new Dictionary<MessageType, Action<object>>();

        localRoutingTable[MessageType.imu] = (payload) => HandleIMUData(payload);
        localRoutingTable[MessageType.tcpLogg] = (payload) => Debug.Log($"TCP Log: {payload}");
        localRoutingTable[MessageType.espLogg] = (payload) => Debug.Log($"ESP Log: {payload}");
        localRoutingTable[MessageType.trackerPreview] = (texture2D) => GUIQueueContainer.GUIqueue.Enqueue((Texture2D)texture2D);
        localRoutingTable[MessageType.eyePreview] = (texture2D) => GUIQueueContainer.GUIqueue.Enqueue((Texture2D)texture2D);

        return localRoutingTable;
    }

    public static Dictionary<string, string> CreateModuleRoutingTable()
    {
        var moduleRoutingTable = new Dictionary<string, string>();

        switch (Configuration.currentVersion)
        {
            case VRMode.Testbed:
                moduleRoutingTable["camera"] = "TCP";
                moduleRoutingTable["rightCrop"] = "TCP";
                moduleRoutingTable["leftCrop"] = "TCP";
                moduleRoutingTable["tracker"] = "TCP";
                moduleRoutingTable["gaze"] = "TCP";
                break;

            case VRMode.UserVR:
                moduleRoutingTable["camera"] = "Serial";
                moduleRoutingTable["rightCrop"] = "Serial";
                moduleRoutingTable["leftCrop"] = "Serial";
                moduleRoutingTable["tracker"] = "TCP";
                moduleRoutingTable["gaze"] = "Serial";
                break;
        }

        return moduleRoutingTable;
    }

    private static void HandleIMUData(object payload)
    {
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
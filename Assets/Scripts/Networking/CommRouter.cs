using System;
using System.Collections.Generic;
using UnityEngine;
using Contracts;
using System.Collections;
using Newtonsoft.Json;


// Communication routing layer.
// Routes messages to the correct transport based on the active VRMode.
public class CommRouter : MonoBehaviour
{
    private Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)> routingTable;
    private Dictionary<MessageType, Action<object>> localRoutingTable;
    private TCP tcpModule;
    private Serial serialModule;
    private IMainThreadQueue _IMainThreadQueue; // Reference to the MainThreadQueue script
    private IConfigManagerConnector _IConfigManager; // Reference to the ConfigManager script


    // Initialize the CommRouter after ConfigManager provides mode.
    public void Initialize(TCP tcp, Serial serial)
    {
        tcpModule = tcp;
        serialModule = serial;

        StartCoroutine(WaitForConnectionCoroutine());
    }


    private IEnumerator WaitForConnectionCoroutine()
    {
        // This coroutine waits until all dependencies are injected before connecting peripherals.

        while (_IMainThreadQueue == null || _IConfigManager == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (tcpModule != null)
        {
            tcpModule.InjectHardwareRouter(this);
        }

        if (serialModule != null)
        {
            serialModule.InjectHardwareRouter(this);
        }

        routingTable = RoutingTable.CreateRoutingTable();
        localRoutingTable = RoutingTable.CreateLocalRoutingTable();
        Debug.Log($"CommRouter initialized.");
    }


    public void InjectModules(IMainThreadQueue MainThreadQueue, IConfigManagerConnector configManager)
    {
        // This method injects dependencies into the HardwareRouter.

        _IMainThreadQueue = MainThreadQueue;
        _IConfigManager = configManager;
    }


    public void RouteMessage(object payload, MessageType type)
    {
        (
            TransportSource transportSource,
            TransportTarget transportTarget,
            FormatType formatType,
            bool isExistingRoute
        ) = DecomposeRoutingTable(type);

        if (!isExistingRoute) return;

        (EncodingType encodeType, bool isValidRoute) = EncodeDecodeLogic(transportSource, transportTarget);

        if (!isValidRoute) return;

        object message;

        switch (encodeType)
        {
            case EncodingType.Encode:
                message = EncodeMessage(payload, formatType);
                break;

            case EncodingType.Decode:
                message = DecodeMessage(payload, formatType);
                break;

            case EncodingType.None:
                message = payload;
                break;

            default:
                Debug.LogError("CommRouter: Unsupported encoding type.");
                return;
        }

        switch (transportTarget)
        {
            case TransportTarget.Tcp:
                tcpModule.SendViaTCP(message, type);
                break;

            case TransportTarget.Serial:
                serialModule.SendViaSerial(message, type);
                break;

            case TransportTarget.Unity:
                RouteInUnity(message, type);
                break;
        }
    }


    private void RouteInUnity(object payload, MessageType messageType)
    {
        // Placeholder for routing messages within Unity.
        if (localRoutingTable.TryGetValue(messageType, out var action))
        {
            action.Invoke(payload);
        }
        else
        {
            Debug.LogWarning($"No local handler for MessageType: {messageType}");
        }
    }


    private (TransportSource, TransportTarget, FormatType, bool isExistingRoute) DecomposeRoutingTable(MessageType messageType)
    {
        // Placeholder for actual decomposition logic.

        if (!routingTable.TryGetValue(messageType, out var route))
        {
            Debug.LogError($"CommRouter: No route defined for {messageType} in {Configuration.currentVersion} mode.");
            return (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON, false);
        }

        var (transportSource, transportTarget, formatType) = route;

        return (transportSource, transportTarget, formatType, true);
    }


    private (EncodingType, bool isValidRoute) EncodeDecodeLogic(TransportSource transportSource, TransportTarget transportTarget)
    {

        if ((int)transportSource == (int)transportTarget)
        {
            Debug.LogError("CommRouter: Transport source and target are the same.");
            return (EncodingType.None, false); // No encoding/decoding needed, invalid route
        }
        else if (transportSource == TransportSource.Unity && transportTarget != TransportTarget.Unity)
        {
            return (EncodingType.Encode, true); // Encode when sending from Unity, valid route
        }
        else if (transportSource != TransportSource.Unity && transportTarget == TransportTarget.Unity)
        {
            return (EncodingType.Decode, true); // Decode when receiving to Unity, valid route
        }
        else if (transportSource != TransportSource.Unity && transportTarget != TransportTarget.Unity)
        {
            return (EncodingType.None, true); // Encode and decode when routing between peripherals
        }
        else
        {
            Debug.LogError("CommRouter: Unsupported transport source/target combination.");
            return (EncodingType.None, false);
        }
    }


    private byte[] EncodeMessage(object payload, FormatType format)
    {
        switch (format)
        {
            case FormatType.JSON:
                string json = JsonConvert.SerializeObject(payload);
                return System.Text.Encoding.UTF8.GetBytes(json);
            case FormatType.PNG:
                if (payload is not Texture2D)
                {
                    Debug.LogError("CommRouter: Payload is not a Texture2D for PNG encoding.");
                    return null;
                }
                var pngTexture = (Texture2D)payload;
                return pngTexture.EncodeToPNG();
            case FormatType.JPEG:
                if (payload is not Texture2D)
                {
                    Debug.LogError("CommRouter: Payload is not a Texture2D for JPEG encoding.");
                    return null;
                }
                var jpegTexture = (Texture2D)payload;
                return jpegTexture.EncodeToJPG();
            default:
                Debug.LogError("CommRouter: Unsupported format type for encoding.");
                return null;
        }
    }


    private object DecodeMessage(object payload, FormatType format)
    {
        switch (format)
        {
            case FormatType.JSON:
                return (string)payload;

            case FormatType.PNG:
                if (payload is not byte[])
                {
                    Debug.LogError("CommRouter: Payload is not a byte array for PNG decoding.");
                    return null;
                }
                var pngData = (byte[])payload;
                var pngTexture = new Texture2D(2, 2);
                if (!pngTexture.LoadImage(pngData))
                {
                    Debug.LogError("CommRouter: Failed to decode PNG image.");
                    return null;
                }
                return pngTexture;

            case FormatType.JPEG:
                if (payload is not byte[])
                {
                    Debug.LogError("CommRouter: Payload is not a byte array for JPEG decoding.");
                    return null;
                }
                var jpegData = (byte[])payload;
                var jpegTexture = new Texture2D(2, 2);
                if (!jpegTexture.LoadImage(jpegData))
                {
                    Debug.LogError("CommRouter: Failed to decode JPEG image.");
                    return null;
                }
                return jpegTexture;

            default:
                Debug.LogError("CommRouter: Unsupported format type for decoding.");
                return null;
        }
    }
}

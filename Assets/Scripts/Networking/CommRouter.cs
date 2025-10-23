using System;
using System.Collections.Generic;
using UnityEngine;
using Contracts;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;


public class CommRouter : MonoBehaviour
{
    // Communication routing layer.
    // Routes messages to the correct transport based on the active VRMode.
    // Handles encoding/decoding of messages as needed.
    // Uses a routing table to determine how to route each message type.

    private TCP tcpModule;
    private Serial serialModule;
    private Dictionary<MessageType, (TransportSource, TransportTarget, FormatType)> globalRoutingTable
    = RoutingTable.CreateGlobalRoutingTable();
    private Dictionary<MessageType, Action<object>> localRoutingTable
    = RoutingTable.CreateLocalRoutingTable();
    private Thread routingThread; // Thread for receiving data from the server

    void Start()
    {
        routingThread = new Thread(DequeueMessage) { IsBackground = true, Name = "CommRouter.Route" };
        routingThread.Start();
    }

    public void Initialize(TCP tcp, Serial serial)
    {
        // Initialize the CommRouter after ConfigManager provides mode.

        tcpModule = tcp;
        serialModule = serial;

        if (tcpModule != null)
        {
            tcpModule.InjectHardwareRouter(this);
        }

        if (serialModule != null)
        {
            serialModule.InjectHardwareRouter(this);
        }
    }


    private void OnApplicationQuit()
    {
        // Clean up the routing thread on application exit.

        RouteQueueContainer.routeQueue.CompleteAdding();
        routingThread?.Join(1000); // Wait for the thread to finish
    }

    private void DequeueMessage()
    {
        // Dequeue a message from the route queue.
        // This function is called in the routing thread.
        foreach (var item in RouteQueueContainer.routeQueue.GetConsumingEnumerable())
        {
            var (payload, type) = item;
            RouteMessage(payload, type);
        }
    }

    private void RouteMessage(object payload, MessageType type)
    {
        // Main routing function.
        // Determines transport source/target, encoding/decoding needs, and routes message.

        // Decompose routing table entry
        (
            TransportSource transportSource,
            TransportTarget transportTarget,
            FormatType formatType,
            bool isExistingRoute
        ) = DecomposeRoutingTable(type);
        if (!isExistingRoute) return;


        // Determine if encoding/decoding is needed
        (EncodingType encodeType, bool isValidRoute) = EncodeDecodeLogic(transportSource, transportTarget);
        if (!isValidRoute) return;

        // Perform encoding/decoding as needed
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

        // Route message to the appropriate transport
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
        // Route message to local Unity handlers

        // Invoke the appropriate local handler based on message type
        // Actions are defined in the localRoutingTable
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
        // Look up the routing table entry for the given message type

        if (!globalRoutingTable.TryGetValue(messageType, out var route))
        {
            Debug.LogError($"CommRouter: No route defined for {messageType} in {Configuration.currentVersion} mode.");
            return (TransportSource.Tcp, TransportTarget.Unity, FormatType.JSON, false);
        }

        var (transportSource, transportTarget, formatType) = route;

        return (transportSource, transportTarget, formatType, true);
    }


    private (EncodingType, bool isValidRoute) EncodeDecodeLogic(TransportSource transportSource, TransportTarget transportTarget)
    {
        // Determine if encoding/decoding is needed based on transport source/target

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
        // Encode the payload based on the specified format

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
        // Decode the payload based on the specified format

        var bytePayload = payload as byte[];
        if (bytePayload == null)
        {
            Debug.LogError("[CommRouter] DecodeMessage expected byte[] payload.");
            return null;
        }

        switch (format)
        {
            case FormatType.JSON:
                string json = System.Text.Encoding.UTF8.GetString(bytePayload);
                return json;
            case FormatType.PNG:
            case FormatType.JPEG:
                var images = ImageDecoder.Decode(bytePayload);
                return images;
            default:
                Debug.LogError("CommRouter: Unsupported format type for encoding.");
                return null;
        }
    }
}

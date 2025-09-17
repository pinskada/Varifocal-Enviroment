using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Contracts;
using System.Threading.Tasks;
using System.Collections;



// Communication routing layer.
// Routes messages to the correct transport based on the active VRMode.
public class CommRouter : MonoBehaviour
{
    public VRMode ActiveMode { get; private set; }

    private Dictionary<MessageType, TransportType> routingTable;

    private TCP tcpModule;
    private Serial serialModule;
    private IMainThreadQueue _IMainThreadQueue; // Reference to the MainThreadQueue script
    private IConfigManagerConnector _IConfigManager; // Reference to the ConfigManager script

    // Event: when a message arrives from hardware
    public event Action<MessageType, byte[]> OnMessageReceived;

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

        ActiveMode = _IConfigManager.GetVRType();

        if (tcpModule != null)
        {
            tcpModule.InjectHardwareRouter(this);
        }

        if (serialModule != null)
        {
            serialModule.InjectHardwareRouter(this);
        }

        SetupRoutingTable();
        Debug.Log($"CommRouter initialized in {ActiveMode} mode.");
    }

    public void InjectModules(IMainThreadQueue MainThreadQueue, IConfigManagerConnector configManager)
    {
        // This method injects dependencies into the HardwareRouter.

        _IMainThreadQueue = MainThreadQueue;
        _IConfigManager = configManager;
    }

    // Defines hardcoded routing profiles for each mode.
    private void SetupRoutingTable()
    {
        routingTable = new Dictionary<MessageType, TransportType>();

        switch (ActiveMode)
        {
            case VRMode.Testbed:
                routingTable[MessageType.EyeImage] = TransportType.Tcp;
                routingTable[MessageType.IMU] = TransportType.Tcp;
                routingTable[MessageType.GazeDistance] = TransportType.Tcp;
                routingTable[MessageType.VarifocalControl] = TransportType.Tcp;
                break;

            case VRMode.UserVR:
                routingTable[MessageType.EyeImage] = TransportType.Serial;
                routingTable[MessageType.IMU] = TransportType.Serial;
                routingTable[MessageType.GazeDistance] = TransportType.Serial;
                routingTable[MessageType.VarifocalControl] = TransportType.Serial;
                break;

            default:
                Debug.LogError($"CommRouter: Unsupported VRMode {ActiveMode}");
                break;
        }
    }

    // Send a message. The router decides which transport to use.
    public void Send(MessageType type, byte[] payload)
    {
        if (!routingTable.TryGetValue(type, out var transport))
        {
            Debug.LogWarning($"CommRouter: No route defined for {type} in {ActiveMode} mode.");
            return;
        }

        switch (transport)
        {
            case TransportType.Tcp:
                if (tcpModule != null)
                {
                    tcpModule.SendViaTCP(Encoding.UTF8.GetString(payload));
                }
                else
                {
                    Debug.LogError("CommRouter: TCP module is null but required.");
                }
                break;

            case TransportType.Serial:
                if (serialModule != null)
                {
                    serialModule.SendViaSerial(Encoding.UTF8.GetString(payload));
                }
                else
                {
                    Debug.LogError("CommRouter: Serial module is null but required.");
                }
                break;
        }
    }

    // Handles incoming messages from the transports.
    // Forwards them via the OnMessageReceived event.
    public void HandleIncoming(TransportType source, MessageType type, byte[] payload)
    {
        Debug.Log($"CommRouter: Received {type} ({payload.Length} bytes) from {source}");

        // Forward message to higher-level systems
    }

    public void SendTCPConfig()
    {
        // Placeholder for sending TCP configuration.
        return;
    }
}

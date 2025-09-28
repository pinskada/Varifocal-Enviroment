using UnityEngine;
using Contracts;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;

// This class handles communication between the Unity application and external devices like Raspberry Pi or ESP32 or local EyeTracker.
// It can run in two modes: testbed - connects to a RPI as client or real - connects to the ESP32 via serial port and creates
// a local TCP server for the EyeTracker.

public class NetworkManager : MonoBehaviour, IModuleSettingsHandler
{
    private TCP tcp; // Reference to the TCP script
    private Serial serial; // Reference to the Serial script
    private CommRouter commRouter; // Reference to the CommRouter script
    private List<RoutingEntry> tcpRoutingList = RoutingTable.CreateTCPModuleRoutingList();
    private List<RoutingEntry> serialRoutingList = RoutingTable.CreateSerialModuleRoutingList();

    public void SettingsChanged(string moduleName, string fieldName)
    {
        // Sends a config message using commrouter to the appropriate module when a setting changes.

        // First check TCP list
        var entry = tcpRoutingList.Find(e => e.Name == moduleName);
        if (entry != null)
        {
            var settingsBlock = entry.GetSettings();
            var field = settingsBlock.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var payload = BuildConfigMessage(settingsBlock, moduleName, field);
                commRouter.RouteMessage(payload, MessageType.tcpConfig);
                return;
            }
        }

        // Then check Serial list
        entry = serialRoutingList.Find(e => e.Name == moduleName);
        if (entry != null)
        {
            var settingsBlock = entry.GetSettings();
            var field = settingsBlock.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var payload = BuildConfigMessage(settingsBlock, moduleName, field);
                serial.SendViaSerial(payload, MessageType.espConfig);
                return;
            }
        }

        Debug.LogWarning($"[NetworkManager] Could not find {moduleName}.{fieldName} in routing lists.");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Initializes TCP client or server and serial port based on the setup.
        ConnectPeripherals();
    }

    void Start()
    {
        // Placeholder for any initialization that needs to occur after Awake.
        if (tcp != null)
        {
            new Thread(tcp.StartTCP) { IsBackground = true, Name = "TCP.Startup" }.Start();
        }
        if (serial != null)
        {
            // Placeholder for starting serial communication if needed.
        }
    }

    private void OnApplicationQuit()
    {
        // This method kills tcp client and serial connection

        tcp.Shutdown();
        if (serial != null)
            serial.Shutdown();
    }


    private void ConnectPeripherals()
    {
        // This method connects peripherals after all dependencies are injected.


        tcp = new TCP(this);

        if (Configuration.currentVersion == VRMode.UserVR)
        {
            serial = new Serial(this);
        }
    }

    public (TCP, Serial) GetCommunicatorInstance()
    {
        return (tcp, serial);
    }


    public void SendTCPConfig()
    {
        foreach (var entry in tcpRoutingList)
        {
            var settingsBlock = entry.GetSettings();
            if (settingsBlock == null) continue;

            var type = settingsBlock.GetType();

            // Fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var payload = BuildConfigMessage(settingsBlock, entry.Name, field);

                commRouter.RouteMessage(payload, MessageType.tcpConfig);
            }
        }
    }

    public void SendSerialConfig()
    {
        foreach (var entry in serialRoutingList)
        {
            var settingsBlock = entry.GetSettings();
            if (settingsBlock == null) continue;

            var type = settingsBlock.GetType();

            // Fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var jsonMessage = BuildConfigMessage(settingsBlock, entry.Name, field);

                serial.SendViaSerial(jsonMessage, MessageType.espConfig);
            }
        }
    }

    private Dictionary<string, object> BuildConfigMessage(object settingsBlock, string moduleName, FieldInfo field)
    {
        var value = field.GetValue(settingsBlock);

        var payload = new Dictionary<string, object>
        {
            { $"{moduleName}.{field.Name}", value }
        };

        return payload;
    }
}

using UnityEngine;
using Contracts;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;


public class NetworkManager : MonoBehaviour, IModuleSettingsHandler
{
    // Manages network communication via TCP and Serial based on VRMode.
    // Initializes and provides access to TCP and Serial modules.
    // Routes configuration changes to the appropriate module via CommRouter.


    private TCP tcp; // Reference to the TCP script
    private Serial serial; // Reference to the Serial script
    [SerializeField] private CommRouter commRouter; // Reference to the CommRouter script
    private List<RoutingEntry> tcpRoutingList = RoutingTable.CreateTCPModuleRoutingList();
    private List<RoutingEntry> serialRoutingList = RoutingTable.CreateSerialModuleRoutingList();


    void Awake()
    {
        // Initializes TCP client or server and serial port based on the setup and
        // injects them into CommRouter.

        tcp = new TCP(this);

        if (Configuration.currentVersion == VRMode.UserVR)
        {
            serial = new Serial(this);
        }
    }


    void Start()
    {
        commRouter.Initialize(tcp, serial);

        // Activates TCP client or server and serial port based on the setup.
        if (tcp != null)
        {
            new Thread(tcp.StartTCP) { IsBackground = true, Name = "TCP.Startup" }.Start();
        }
        if (serial != null)
        {
            // Placeholder for starting serial communication if needed.
        }
    }


    public void InjectModules(CommRouter commRouter)
    {
        // Injects the CommRouter instance into NetworkManager.
        this.commRouter = commRouter;
    }


    // Returns the instances of TCP and Serial for Bootstrapper to bind them to ConfigManager.
    public (TCP, Serial) GetCommunicatorInstance() => (tcp, serial);


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
                RouteQueueContainer.routeQueue.Add((payload, MessageType.tcpConfig));
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
                RouteQueueContainer.routeQueue.Add((payload, MessageType.espConfig));
                return;
            }
        }

        Debug.LogWarning($"[NetworkManager] Could not find {moduleName}.{fieldName} in routing lists.");
    }


    private Dictionary<string, object> BuildConfigMessage(object settingsBlock, string moduleName, FieldInfo field)
    {
        // Constructs a config message payload for a specific field in a settings block.
        // Returns a dictionary with the format { "ModuleName.FieldName": value }.

        if (settingsBlock == null) Debug.LogWarning($"[NetworkManager] BuildConfigMessage expected settingsBlock, but got null.");
        if (moduleName == null) Debug.LogWarning($"[NetworkManager] BuildConfigMessage expected moduleName, but got null.");
        if (field == null) Debug.LogWarning($"[NetworkManager] BuildConfigMessage expected field, but got null.");


        var value = field.GetValue(settingsBlock);

        var payload = new Dictionary<string, object>
        {
            { $"{moduleName}.{field.Name}", value }
        };

        return payload;
    }


    public void SendTCPConfig()
    {
        // Sends the current TCP configuration to the connected device.
        // Iterates through all routing entries for TCP and sends each setting from each module.

        foreach (var entry in tcpRoutingList)
        {
            var settingsBlock = entry.GetSettings();
            if (settingsBlock == null) continue;

            var type = settingsBlock.GetType();

            // Fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var payload = BuildConfigMessage(settingsBlock, entry.Name, field);

                RouteQueueContainer.routeQueue.Add((payload, MessageType.tcpConfig));
            }
        }
        var configReadyMsg = "ConfigReady";
        RouteQueueContainer.routeQueue.Add((configReadyMsg, MessageType.configReady));
    }


    public void SendSerialConfig()
    {
        // Sends the current Serial configuration to the connected device.
        // Iterates through all routing entries for Serial and sends each setting from each module.

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
        var configReadyMsg = "ConfigReady";
        RouteQueueContainer.routeQueue.Add((configReadyMsg, MessageType.configReady));
    }


    private void OnApplicationQuit()
    {
        // This method kills tcp client and serial connections
        if (tcp != null) tcp.Shutdown();
        if (serial != null) serial.Shutdown();
    }
}

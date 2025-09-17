using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using Contracts;
using System.Threading;
using System.Threading.Tasks;

// This class handles communication between the Unity application and external devices like Raspberry Pi or ESP32 or local EyeTracker.
// It can run in two modes: testbed - connects to a RPI as client or real - connects to the ESP32 via serial port and creates
// a local TCP server for the EyeTracker.

public class NetworkManager : MonoBehaviour
{
    [SerializeField] private bool isTestbed = true; // Flag to indicate if this is a testbed environment
    //private GuiHub guiHub; // Reference to the GuiHub script
    private TCP tcp; // Reference to the TCP script
    private Serial serial; // Reference to the Serial script
    private IIMUHandler _IIMUHandler; // Reference to the IMUHandler script
    private IMainThreadQueue _IMainThreadQueue; // Reference to the MainThreadQueue script
    private IConfigManagerConnector _IConfigManager; // Reference to the ConfigManager script

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initializes TCP client or server and serial port based on the setup.


        StartCoroutine(WaitForConnectionCoroutine());

    }

    async Task OnApplicationQuit()
    {
        // This method kills tcp client and serial connection

        await tcp.Shutdown();
        if (serial != null)
            serial.Shutdown();
    }


    private IEnumerator WaitForConnectionCoroutine()
    {
        // This coroutine waits until all dependencies are injected before connecting peripherals.


        while (_IIMUHandler == null || _IMainThreadQueue == null || _IConfigManager == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        ConnectPeripherals();
    }


    private void ConnectPeripherals()
    {
        // This method connects peripherals after all dependencies are injected.


        tcp = new TCP(this, _IConfigManager, _IMainThreadQueue, isTestbed);
        new Thread(tcp.StartTCP) { IsBackground = true, Name = "TCP.Startup" }.Start();

        if (!isTestbed)
        {
            serial = new Serial(this);
        }
    }


    public void InjectModules(IIMUHandler imuHandler,
        IMainThreadQueue MainThreadQueue, IConfigManagerConnector configManager)
    {
        // This method injects dependencies into the NetworkManager.


        _IIMUHandler = imuHandler;
        _IMainThreadQueue = MainThreadQueue;
        _IConfigManager = configManager;
    }


    public void RedirectMessage(int packetType, byte[] payload)
    {
        // This method redirects incoming messages to the appropriate handler based on the packet type.

        // Handle data based on packet type
        switch (packetType)
        {
            case 'J': // JSON packet
                HandleJson(payload);
                break;
            case 'P': // Preview image packet
                HandlePreviewImage(payload);
                break;
            case 'E': // EyeTracker image packet
                HandleEyeTrackerImage(payload);
                break;
            default:
                Debug.LogWarning($"Unknown packet type: {packetType}");
                break;
        }
    }


    public void HandleJson(byte[] payload)
    {
        // This method handles the JSON payload received peripherals.

        string json = Encoding.UTF8.GetString(payload);


        try
        {
            JObject message = JObject.Parse(json);

            string type = message["type"]?.ToString();
            switch (type)
            {

                case "IMU":
                    //UnityEngine.Debug.Log($"9DOF data recieved");

                    if (_IIMUHandler != null)
                    {
                        IMUData imuData = message["data"].ToObject<IMUData>();
                        _IIMUHandler.UpdateFilter(imuData);
                    }
                    else
                    {
                        Debug.LogWarning("IMUHandler is not assigned!");
                    }
                    break;
                case "GazeDistance":
                // Not yet implemented
                default:
                    Debug.LogWarning($"Unknown message type: {type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse JSON: {ex.Message}");
        }

    }

    public void HandlePreviewImage(byte[] payload)
    {
        // This method handles the preview image from RPI.

        //guiHub.HandlePreviewImage(payload);
    }

    public void HandleEyeTrackerImage(byte[] payload)
    {
        // This method handles the eye tracker image from ESP32 and sends it locally to the RPI.

        // Not yet implemented.
    }

    public void SendTCPConfig()
    {
        // This method sends the configuration to the Raspberry Pi or other devices via guiHub->guiInterface.
        /*
                if (isTestbed)
                {
                    guiHub.SendConfigToRpi();
                }
                else
                {
                    guiHub.SendConfigToEsp32();
                    guiHub.SendConfigToLocalEyeTracker();
                }
                */
    }

    public void SendMessage()
    {
        // This method sends messages to the appropriate handler based on the current setup.

        if (isTestbed)
        {
            // Not yet implemented
        }
        else
        {
            // Not yet implemented
        }
    }
}

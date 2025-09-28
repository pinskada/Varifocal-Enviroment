using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using Contracts;
using System.Threading;

// This class handles communication between the Unity application and external devices like Raspberry Pi or ESP32 or local EyeTracker.
// It can run in two modes: testbed - connects to a RPI as client or real - connects to the ESP32 via serial port and creates
// a local TCP server for the EyeTracker.

public class NetworkManager : MonoBehaviour, IModuleSettingsHandler
{
    private TCP tcp; // Reference to the TCP script
    private Serial serial; // Reference to the Serial script
    private IIMUHandler _IIMUHandler; // Reference to the IMUHandler script
    private IMainThreadQueue _IMainThreadQueue; // Reference to the MainThreadQueue script
    private IConfigManagerConnector _IConfigManager; // Reference to the ConfigManager script

    public void SettingsChanged(string moduleName)
    {

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
        // Placeholder for sending TCP configuration.
        return;
    }

    public void SendSerialConfig()
    {
        // Placeholder for sending Serial configuration.
        return;
    }
}

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


    private void OnApplicationQuit()
    {
        // This method kills tcp client and serial connection

        tcp.Shutdown();
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


        tcp = new TCP(this, _IConfigManager);
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

    public void SendTCPConfig()
    {
        // Placeholder for sending TCP configuration.
        return;
    }
}

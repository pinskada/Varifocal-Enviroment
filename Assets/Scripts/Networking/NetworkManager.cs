using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;

// This class handles communication between the Unity application and external devices like Raspberry Pi or ESP32 or local EyeTracker.
// It can run in two modes: testbed - connects to a RPI as client or real - connects to the ESP32 via serial port and creates
// a local TCP server for the EyeTracker.

public class NetworkManager : MonoBehaviour
{
    [SerializeField] private bool isTestbed = true; // Flag to indicate if this is a testbed environment
    private GuiHub guiHub; // Reference to the GuiHub script
    private TCP tcp; // Reference to the TCP script
    private Serial serial; // Reference to the Serial script
    [SerializeField] private IMUHandler imuHandler; // Reference to the IMUHandler script

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initializes TCP client or server and serial port based on the setup.
        
        StartCoroutine(FindGuiReferences());
        tcp = new TCP(isTestbed, this);
        if (!isTestbed)
        {
            serial = new Serial(this);
        }
    }

    void OnApplicationQuit()
    {
        // This method kills tcp client and serial connection

        tcp.Shutdown();
        if (serial != null)
            serial.Shutdown();
    }

    private IEnumerator FindGuiReferences()
    {
        // This method attempts to find the GuiRenderer and GuiHub references in the scene.

        int linkAttempts = 0;
        while (linkAttempts < 50)
        {
            yield return new WaitForSeconds(0.1f);

            if (guiHub == null)
                guiHub = FindFirstObjectByType<GuiHub>();
            else 
                break;

            linkAttempts++;
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

                    if (imuHandler != null)
                    {
                        JToken imuData = message["data"];
                        imuHandler.UpdateFilter(imuData);
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

        guiHub.HandlePreviewImage(payload);
    }

    public void HandleEyeTrackerImage(byte[] payload)
    {
        // This method handles the eye tracker image from ESP32 and sends it locally to the RPI.

        // Not yet implemented.
    }

    public void SendConfig()
    {
        // This method sends the configuration to the Raspberry Pi or other devices via guiHub->guiInterface.

        if (isTestbed)
        {
            guiHub.SendConfigToRpi();
        }
        else
        {
            guiHub.SendConfigToEsp32();
            guiHub.SendConfigToLocalEyeTracker();
        }
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

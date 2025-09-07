using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using Contracts;
using UnityEngine;

public class TCP
{
    // This class handles TCP connection either to the Raspberry Pi (RPI) by connecting
    // to the RPI server or local, both as a client.
    // For RPI connection, a static IP is set.

    private IConfigManagerConnector _IConfigManager;
    private NetworkManager networkManager; // Reference to the NetworkManager script
    private string moduleName = "TCPSettings"; // Name of the module for configuration
    private bool isConnected = false;
    private volatile bool isShuttingDown = false;
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private MemoryStream incomingStream = new MemoryStream();
    private bool isTestbed;

    //*********************************************************************************************
    // Constants imported from ConfigManager
    public string ipAddress; // Static IP for testbed
    public string raspberryPiIP; // RPI IP for testbed
    public string localIP; // Local IP for serial communication
    public string subnetMask; // Subnet mask for static IP
    public string adapterName; // Name of the network adapter to set static IP
    public string netshFileName; // Path to netsh executable
    public int port; // Port number for TCP connection
    public int readBufferSize; // Size of the buffer for incoming data
    //*********************************************************************************************


    public TCP(bool isTestbed = false, NetworkManager networkManager = null)
    {
        // This constructor is used to create a TCP instance for the test bed.

        this.isTestbed = isTestbed;
        this.networkManager = networkManager;
    }


    public void InjectModules(IConfigManagerConnector configManager)
    {
        // Inject the external modules interfaces into this handler

        _IConfigManager = configManager;
    }


    public IEnumerator WaitForConnectionCoroutine()
    {
        while (_IConfigManager == null)
        {
            yield return null; // Wait until everything is assigned
        }

        _IConfigManager.BindModule(this, moduleName); // Bind this module to the config manager

        UnityEngine.Debug.Log("All components and settings initialized.");

        if (isTestbed)
        {
            ConfigureIPmode(true); // Set static IP to communicate with the RPI on a local network.
            yield return new WaitForSeconds(5f); // Wait for a few seconds to ensure the IP is set
        }

        UnityEngine.Debug.Log("Attempting to connect to a server...");
        ConnectToServer(); // Connect to the RPI TCP server
    }


    public void Shutdown()
    {
        // This method cleans up the client resources when the application quits.

        isShuttingDown = true; // Set the flag to true to prevent errors during shutdown

        DisconnectFromServer(); // Disconnect from the server when the application quits

        if (isTestbed)
            ConfigureIPmode(false); // Reset the IP to DHCP
    }


    private void ConfigureIPmode(bool setStatic)
    {
        // This method sets static IP to be able to communicate with the RPI on a local network.
        // IN ORDER TO WORK THESE SETTINGS MUST BE ENSURED: Edit -> Project Settings -> Player -> 
        // -> Configuration -> Scripting Backend: Mono; Api Compatibility Level: .NET Framework


        // Validate inputs
        if (string.IsNullOrWhiteSpace(adapterName) ||
            string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(subnetMask))
        {
            UnityEngine.Debug.LogError("[TCP] ConfigureIPmode: missing adapter/IP/mask.");
            return;
        }

        string args = setStatic
            ? $"interface ipv4 set address name=\"{adapterName}\" source=static address={ipAddress} mask={subnetMask} gateway=none"
            : $"interface ipv4 set address name=\"{adapterName}\" source=dhcp";
        /* OLD VERSION if the new one does not work
        if (setStatic)
            args = $"interface ip set address name=\"{adapterName}\" static {ipAddress} {subnetMask} {gateway} 1";
        else
            args = $"interface ip set address name=\"{adapterName}\" source=dhcp";
        */

        // Start the netsh process with elevated privileges
        var setStaticIProcess = new ProcessStartInfo {
            FileName = netshFileName,
            Arguments = args,
            UseShellExecute = false,     // fine since you already run as admin
            CreateNoWindow = true
        };

        /* OLD VERSION if the new one does not work
        {
            FileName = @"C:\Windows\System32\netsh.exe", // Path to netsh executable
            Arguments = args, // Arguments for the command
            Verb = "runas", // This is required to run as administrator
            UseShellExecute = false, // This is required to redirect output
            RedirectStandardOutput = true, // Redirect output to read it
            RedirectStandardError = true, // Redirect error to read it
            CreateNoWindow = true // Don't create a window
        };
        */

        // Try to start the process
        try
        {
            using (var p = Process.Start(setStaticIProcess))
            {
                bool exited = p.WaitForExit(15000);  // Timeout to avoid hangs

                if (!exited)
                {
                    UnityEngine.Debug.LogError("Netsh did not exit within 15s. Args: " + args);
                    return;
                }
                if (p.ExitCode != 0)
                    UnityEngine.Debug.LogError($"Netsh failed (exit {p.ExitCode}). Args: {args}");
                else
                    UnityEngine.Debug.Log(setStatic ? "Static IP set." : "IP reset to DHCP.");
                }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Netsh error: " + ex.Message);
        }
    }


    private void ConnectToServer()
    {
        // This method connects to either the RPI TCP server or local server.

        try
        {
            // Create a new TcpClient and connect to the server
            client = new TcpClient();
            if (isTestbed)
            {
                // Connect to the Raspberry Pi IP address
                client.Connect(raspberryPiIP, port);
            }
            else
            {
                // Connect to the local IP address for serial communication
                client.Connect(localIP, port);
            }

            // Get the network stream for reading and writing data
            stream = client.GetStream();
            isConnected = true;

            if (isTestbed)
                UnityEngine.Debug.Log("Connected to Raspberry Pi at " + raspberryPiIP + ":" + port);
            else
                UnityEngine.Debug.Log("Connected to local server at " + localIP + ":" + port);

            // Start the receive thread to listen for incoming messages
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            if (networkManager != null)
                networkManager.SendRPIConfig(); // Send initial configuration to the RPI
            else
                UnityEngine.Debug.LogError("NetworkManager reference is null, cannot send config to over TCP.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Connection failed: " + e.Message);
        }
    }


    private void DisconnectFromServer()
    {
        // This method disconnects from the TCP server.

        if (!isConnected) return;
        isConnected = false;

        try
        {
            stream?.Close();
            stream?.Dispose();
        }
        catch (Exception e) { UnityEngine.Debug.LogWarning("Stream close failed: " + e.Message); }

        try
        {
            client?.Close();
            client?.Dispose();
        }
        catch (Exception e) { UnityEngine.Debug.LogWarning("Client close failed: " + e.Message); }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(500); // Allow thread to exit cleanly
        }

        receiveThread = null;
        stream = null;
        client = null;

        UnityEngine.Debug.Log("Disconnected from RPI.");
    }


    public void SendViaTCP(string message)
    {
        // This method sends a message to the TCP server.

        // Check if the client is connected and the stream is not null
        if (!isConnected || stream == null)
        {
            UnityEngine.Debug.LogWarning("Can not send a message, not connected to server.");
            return;
        }

        try
        {
            // Convert the message to bytes and send it over the stream
            // Append a newline character to the message to indicate the end of the message
            byte[] data = Encoding.UTF8.GetBytes(message + "\\n"); // \n
            stream.Write(data, 0, data.Length);
            UnityEngine.Debug.Log("Sent: " + message);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Send failed: " + e.Message);
        }
    }


    private void ReceiveData()
    {
        // This method receives data from the RPI TCP server.

        // Buffer for incoming data
        byte[] buffer = new byte[readBufferSize];

        try
        {
            // Keep listening for incoming messages until the connection is closed
            while (isConnected)
            {
                // Read data from the stream into the buffer
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                // Decode the incoming packet
                HandleIncomingData(buffer, bytesRead);
            }
        }
        catch (Exception e)
        {
            // Prevent errors during shutdown
            if (!isShuttingDown)
                UnityEngine.Debug.LogError("Receive error: " + e.Message);
        }
    }


    private void HandleIncomingData(byte[] data, int length)
    {
        // This method handles incoming data from the RPI TCP server.

        // Write new incoming bytes
        incomingStream.Seek(0, SeekOrigin.End);
        incomingStream.Write(data, 0, length);

        incomingStream.Position = 0; // Start reading from beginning of buffer

        while (true)
        {
            if (incomingStream.Length - incomingStream.Position < 4)
            {
                // Not enough bytes for header
                //UnityEngine.Debug.Log("Not enough bytes for header, waiting for more data...");
                break;
            }

            long packetStartPos = incomingStream.Position;

            byte typeByte = (byte)incomingStream.ReadByte();
            char packetType = (char)typeByte;

            byte[] lengthBytes = new byte[3];
            incomingStream.Read(lengthBytes, 0, 3);
            int payloadLength = (lengthBytes[0] << 16) | (lengthBytes[1] << 8) | lengthBytes[2];

            if (incomingStream.Length - incomingStream.Position < payloadLength)
            {
                // Not enough bytes for full payload
                incomingStream.Position = packetStartPos; // rewind back, wait for more data
                //UnityEngine.Debug.Log("Not enough bytes for full payload, waiting for more data...");
                break;
            }

            // Read full payload
            byte[] payload = new byte[payloadLength];
            incomingStream.Read(payload, 0, payloadLength);

            networkManager.RedirectMessage(packetType, payload);
        }

        // Clean up already processed bytes
        long leftoverBytes = incomingStream.Length - incomingStream.Position;
        if (leftoverBytes > 0)
        {
            byte[] leftover = new byte[leftoverBytes];
            incomingStream.Read(leftover, 0, (int)leftoverBytes);
            incomingStream.SetLength(0);
            incomingStream.Write(leftover, 0, leftover.Length);
        }
        else
        {
            incomingStream.SetLength(0);
        }
    }
}

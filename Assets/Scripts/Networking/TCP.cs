using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;

public class TCP
{
    // This class handles TCP connection either to the Raspberry Pi (RPI) by connecting
    // to the RPI server as a client or creates a server for local connection.
    // For RPI connection, a static IP is set.

    private NetworkManager networkManager; // Reference to the NetworkManager script
    private bool isTestbed;
    private string raspberryPiIP = "192.168.2.2";
    private string localIP = "127.0.0.1";
    private int port = 65432;
    private bool isConnected = false;
    private volatile bool isShuttingDown = false;
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private MemoryStream incomingStream = new MemoryStream();


    public TCP(bool isTestbed = false, NetworkManager networkManager = null)
    {
        // This constructor is used to create a TCP instance for the test bed.

        this.isTestbed = isTestbed;
        this.networkManager = networkManager;

        if (isTestbed)
        {
            SetStaticIP(); // Set static IP to communicate with the RPI on a local network.
            Thread.Sleep(5000); // Wait for a few seconds to ensure the IP is set
        }

        UnityEngine.Debug.Log("Attempting to connect to a server...");
        ConnectToServer(); // Connect to the RPI TCP server
    }

    public void Shutdown()
    {
        // This method cleans up the client resources when the application quits.

        isShuttingDown = true; // Set the flag to true to prevent errors during shutdown

        Disconnect(); // Disconnect from the server when the application quits

        if (isTestbed)
            ResetToDHCP(); // Reset the IP to DHCP
    }

    private static void SetStaticIP()
    {
        // This method sets static IP to be able to communicate with the RPI on a local network.
        // IN ORDER TO WORK THESE SETTINGS MUST BE ENSURED: Edit -> Project Settings -> Player -> 
        // -> Configuration -> Scripting Backend: Mono; Api Compatibility Level: .NET Framework

        // Adapter parameters
        string adapterName = "Ethernet";
        string ipAddress = "192.168.2.1";
        string subnetMask = "255.255.255.0";
        string gateway = "192.168.2.2";

        // Arguments for netsh command
        string args = $"interface ip set address name=\"{adapterName}\" static {ipAddress} {subnetMask} {gateway} 1";

        // Run the netsh command to set the static IP
        RunNetsh(args, true);        
    }

    private static void ResetToDHCP()
    {
        // This method sets resets the IP back to DHCP.
        // IN ORDER TO WORK THESE SETTINGS MUST BE ENSURED: Edit -> Project Settings -> Player -> 
        // -> Configuration -> Scripting Backend: Mono; Api Compatibility Level: .NET Framework

        // Adapter parameters
        string adapterName = "Ethernet";

        // Arguments for netsh command
        string args = $"interface ip set address name=\"{adapterName}\" source=dhcp";

        // Run the netsh command to reset the IP to DHCP
        RunNetsh(args, false);        
    }

    private static void RunNetsh(string args, bool setStatic)
    {
        // Start the netsh process with elevated privileges
        Process setStaticIProcess = new Process();
        // Set the process start info
        setStaticIProcess.StartInfo = new ProcessStartInfo
        {
            FileName = @"C:\Windows\System32\netsh.exe", // Path to netsh executable
            Arguments = args, // Arguments for the command
            Verb = "runas", // This is required to run as administrator
            UseShellExecute = false, // This is required to redirect output
            RedirectStandardOutput = true, // Redirect output to read it
            RedirectStandardError = true, // Redirect error to read it
            CreateNoWindow = true // Don't create a window
        };

        // Try to start the process
        try
        {
            setStaticIProcess.Start();
            setStaticIProcess.WaitForExit();
            if (setStatic)
                UnityEngine.Debug.Log("Static IP has been set successfully.");
            else
                UnityEngine.Debug.Log("IP has been reset to DHCP successfully.");
        }
        catch (Exception ex)
        {
            if (setStatic)
                UnityEngine.Debug.LogError("Failed to set static IP: " + ex.Message);
            else
                UnityEngine.Debug.LogError("Failed to reset IP back to DHCP: " + ex.Message);
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

            networkManager.SendConfig(); // Send initial configuration to the RPI
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Connection failed: " + e.Message);
        }
    }

    private void Disconnect()
    {
        // This method disconnects from the RPI TCP server.

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
        // This method sends a message to the RPI TCP server.

        // Check if the client is connected and the stream is not null
        if (!isConnected || stream == null) return;

        try
        {
            // Convert the message to bytes and send it over the stream
            // Append a newline character to the message to indicate the end of the message
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
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
        byte[] buffer = new byte[1024];

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

            // Handle data based on packet type
            switch (packetType)
            {
                case 'J': // JSON packet
                    networkManager.HandleJson(payload);
                    break;
                case 'P': // Preview image packet
                    networkManager.HandlePreviewImage(payload);
                    break;
                case 'E': // EyeTracker image packet
                    networkManager.HandleEyeTrackerImage(payload);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"Unknown packet type: {packetType}");
                    break;
            }
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

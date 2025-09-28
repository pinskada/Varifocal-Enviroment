using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using Contracts;


public class TCP : IModuleSettingsHandler
{
    // This class handles TCP connection either to the Raspberry Pi (RPI) by connecting
    // to the RPI server or local, both as a client.
    // For RPI connection, a static IP is set.
    private CommRouter commRouter; // Reference to the CommRouter script
    private NetworkManager networkManager; // Reference to the NetworkManager script
    private volatile bool isConnected = false; // Flag to indicate if the client is connected to the server
    private volatile bool isShuttingDown = false; // Flag to indicate if the application is shutting down
    private TcpClient client; // TcpClient for connecting to the server
    private NetworkStream stream; // NetworkStream for reading and writing data
    private Thread receiveThread; // Thread for receiving data from the server
    private byte[] incomingBuffer;
    private int bufferOffset = 0;  // Start of unprocessed data
    private int bufferCount = 0;   // How many valid bytes are in the buffer
    private int sendRetryCount = 0; // Counter for send retries


    public TCP(NetworkManager networkManager)
    {
        // This constructor is used to create a TCP instance.


        this.networkManager = networkManager;


        UnityEngine.Debug.Log("All components and settings initialized.");
    }

    public void SettingsChanged(string moduleName, string fieldName)
    {
        // This method is called when settings are changed in the ConfigManager.
        // You can implement any necessary actions to handle the updated settings here.
    }

    public void InjectHardwareRouter(CommRouter router)
    {
        // This method injects the CommRouter instance into the TCP module.
        commRouter = router;
    }


    public void Shutdown()
    {
        // This method cleans up the client resources when the application quits.


        isShuttingDown = true; // Set the flag to true to prevent errors during shutdown

        DisconnectFromServer(); // Disconnect from the server when the application quits

        if (Configuration.currentVersion == VRMode.Testbed)
            ConfigureIPmode(false); // Reset the IP to DHCP
    }


    public void StartTCP()
    {
        // This method sets static IP if in testbed mode and connects to the TCP server.


        bool IPisSet = false;

        // Set static IP if in testbed mode
        if (Configuration.currentVersion == VRMode.Testbed)
        {
            IPisSet = ConfigureIPmode(true); // Set static IP to communicate with the RPI on a local network.
        }

        if (!IPisSet && Configuration.currentVersion == VRMode.Testbed)
        {
            UnityEngine.Debug.LogError("IP configuration failed, cannot connect to server.");
            return; // Exit if IP configuration fails
        }

        incomingBuffer = new byte[Settings.TCP.readBufferSize * 4];

        UnityEngine.Debug.Log("Attempting to connect to a server...");
        ConnectToServer(); // Connect to the RPI TCP server
    }


    public bool ConfigureIPmode(bool setStatic)
    {
        // This method sets static IP to be able to communicate with the RPI on a local network.
        // IN ORDER TO WORK THESE SETTINGS MUST BE ENSURED: Edit -> Project Settings -> Player -> 
        // -> Configuration -> Scripting Backend: Mono; Api Compatibility Level: .NET Framework


        // Validate inputs
        if (string.IsNullOrWhiteSpace(Settings.TCP.adapterName) ||
            (setStatic && (string.IsNullOrWhiteSpace(Settings.TCP.ipAddress) || string.IsNullOrWhiteSpace(Settings.TCP.subnetMask))))

        {
            UnityEngine.Debug.LogError("[TCP] ConfigureIPmode: missing adapter/IP/mask.");
            return false;
        }

        string args = setStatic
            ? $"interface ipv4 set address name=\"{Settings.TCP.adapterName}\" source=static address={Settings.TCP.ipAddress} mask={Settings.TCP.subnetMask} gateway=none"
            : $"interface ipv4 set address name=\"{Settings.TCP.adapterName}\" source=dhcp";
        /* OLD VERSION if the new one does not work
        if (setStatic)
            args = $"interface ip set address name=\"{adapterName}\" static {ipAddress} {subnetMask} {gateway} 1";
        else
            args = $"interface ip set address name=\"{adapterName}\" source=dhcp";
        */

        var file = string.IsNullOrWhiteSpace(Settings.TCP.netshFileName) ? "netsh" : Settings.TCP.netshFileName;

        // Start the netsh process with elevated privileges
        var setStaticIProcess = new ProcessStartInfo
        {
            FileName = file,
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
                bool exited = p.WaitForExit(Settings.TCP.IPsetTimeout);  // Timeout to avoid hangs

                if (!exited)
                {
                    UnityEngine.Debug.LogError("Netsh did not exit within 15s. Args: " + args);
                    return false;
                }
                if (p.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"Netsh failed (exit {p.ExitCode}). Args: {args}");
                    return false;
                }
                else
                {
                    UnityEngine.Debug.Log(setStatic ? "Static IP set." : "IP reset to DHCP.");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Netsh error: " + ex.Message);
            return false;
        }
    }


    private void ConnectToServer()
    {
        // This method connects to either the RPI TCP server or localhost.

        try
        {
            // Create a new TcpClient and connect to the server
            client = new TcpClient() { NoDelay = true };
            if (Configuration.currentVersion == VRMode.Testbed)
            {
                // Connect to the Raspberry Pi IP address
                client.Connect(Settings.TCP.raspberryPiIP, Settings.TCP.port);
            }
            else
            {
                // Connect to the local IP address for serial communication
                client.Connect(Settings.TCP.localIP, Settings.TCP.port);
            }

            // Get the network stream for reading and writing data
            stream = client.GetStream();

            // Set timeout for blocking reads
            stream.ReadTimeout = Settings.TCP.readTimeout;
            isConnected = true;

            if (Configuration.currentVersion == VRMode.Testbed)
                UnityEngine.Debug.Log("Connected to Raspberry Pi at " + Settings.TCP.raspberryPiIP + ":" + Settings.TCP.port);
            else
                UnityEngine.Debug.Log("Connected to local server at " + Settings.TCP.localIP + ":" + Settings.TCP.port);

            // Start the receive thread to listen for incoming messages
            receiveThread = new Thread(ReceiveViaTCP) { IsBackground = true, Name = "TCP.Receive" };
            receiveThread.Start();

            if (networkManager != null)
                // Send initial configuration to the RPI
                networkManager.SendTCPConfig();
            else
                UnityEngine.Debug.LogError("NetworkManager reference is null, cannot send config over TCP.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Connecting to to TCP server failed: " + e.Message);
        }
    }


    private void DisconnectFromServer()
    {
        // This method disconnects from the TCP server.

        if (!isConnected) return;
        isConnected = false;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(500); // Allow thread to exit cleanly
        }

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

        receiveThread = null;
        stream = null;
        client = null;

        UnityEngine.Debug.Log("Disconnected from TCP.");
    }


    public void SendViaTCP(object message, MessageType messageType)
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
            byte[] byteMessage = message as byte[];
            byte[] data = EncodeTCPStream(byteMessage, messageType);
            stream.Write(data, 0, data.Length);
            UnityEngine.Debug.Log("Sent: " + message);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Send failed: " + e.Message);
            if (sendRetryCount > Settings.TCP.maxSendRetries)
            {
                sendRetryCount = 0;
                UnityEngine.Debug.LogError("Multiple send failures, disconnecting.");
                isConnected = false; // Assume connection is lost
                return;
            }
            else
            {
                sendRetryCount++;
                SendViaTCP(message, messageType); // Retry sending the message
                UnityEngine.Debug.LogWarning($"Send retry {sendRetryCount}/3");
            }
        }
    }

    private byte[] EncodeTCPStream(byte[] message, MessageType messageType)
    {
        int payloadLength = message.Length;
        byte packetType = (byte)(int)messageType;


        // Create a header: 1 byte type + 3 bytes length
        byte[] header = new byte[4];
        header[0] = packetType;
        header[1] = (byte)((payloadLength >> 16) & 0xFF);
        header[2] = (byte)((payloadLength >> 8) & 0xFF);
        header[3] = (byte)(payloadLength & 0xFF);

        // Combine header and payload
        byte[] payload = new byte[4 + payloadLength];
        Buffer.BlockCopy(header, 0, payload, 0, 4);
        Buffer.BlockCopy(message, 0, payload, 4, payloadLength);

        return payload;
    }

    private void ReceiveViaTCP()
    {
        // This method receives data from the TCP server.


        // Buffer for incoming data
        byte[] buffer = new byte[Settings.TCP.readBufferSize];

        try
        {
            // Keep listening for incoming messages until the connection is closed
            while (isConnected)
            {
                try
                {
                    // Read data from the stream into the buffer
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    // Decode the incoming packet
                    DecodeTCPStream(buffer, bytesRead);
                }
                catch (IOException ex)
                {
                    // This happens when ReadTimeout is reached
                    if (isShuttingDown)
                        break; // Exit quietly during shutdown

                    UnityEngine.Debug.LogWarning("TCP Read timeout: " + ex.Message);
                }
            }
        }
        catch (Exception e)
        {
            // Prevent errors during shutdown
            if (!isShuttingDown)
                UnityEngine.Debug.LogError("Receive error: " + e.Message);
        }
    }


    private void DecodeTCPStream(byte[] data, int length)
    {
        // This method decodes incoming data from the TCP server read by ReceiveData().


        // Append new data to buffer
        if (bufferCount + length > incomingBuffer.Length)
        {
            UnityEngine.Debug.LogError("[TCP] Incoming buffer overflow. Clearing buffer.");
            bufferOffset = 0;
            bufferCount = 0;
            return;
        }

        Array.Copy(data, 0, incomingBuffer, bufferOffset + bufferCount, length);
        bufferCount += length;


        // Process packets
        int readPos = bufferOffset;
        while (bufferCount >= 4) // header = 1 type + 3 length
        {
            byte packetType = incomingBuffer[readPos];
            int payloadLength = (incomingBuffer[readPos + 1] << 16) |
                                (incomingBuffer[readPos + 2] << 8) |
                                incomingBuffer[readPos + 3];

            // Sanity check
            if (payloadLength <= 0 || payloadLength > Settings.TCP.maxPacketSize)
            {
                UnityEngine.Debug.LogError($"Invalid payload length: {payloadLength}, clearing buffer.");
                bufferOffset = 0;
                bufferCount = 0;
                return;
            }

            if (bufferCount < 4 + payloadLength)
            {
                // Not enough data yet → wait for next DecodeTCPData call
                break;
            }

            // Read payload
            byte[] payload = new byte[payloadLength];
            Array.Copy(incomingBuffer, readPos + 4, payload, 0, payloadLength);

            MessageType msgType;

            try
            {
                if (Enum.IsDefined(typeof(MessageType), (int)packetType))
                {
                    msgType = (MessageType)packetType;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("MessageType " + packetType + " not found.");
                    return;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("MessageType parse error: " + e.Message);
                return;
            }

            // Dispatch
            if (commRouter != null)
                commRouter.RouteMessage(payload, msgType);
            else
                UnityEngine.Debug.LogError("CommRouter reference is null, cannot redirect message.");

            // Advance readPos
            readPos += 4 + payloadLength;
            bufferCount -= 4 + payloadLength;
        }

        // Move leftover data to start of buffer
        if (bufferCount > 0 && readPos > bufferOffset)
        {
            Array.Copy(incomingBuffer, readPos, incomingBuffer, 0, bufferCount);
        }

        bufferOffset = 0;
    }
}

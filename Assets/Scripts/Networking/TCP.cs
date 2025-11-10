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
        Thread.Sleep(3500); // Wait for IP to be set

        if (!IPisSet && Configuration.currentVersion == VRMode.Testbed)
        {
            UnityEngine.Debug.LogError("[TCP] IP configuration failed, cannot connect to server.");
            return; // Exit if IP configuration fails
        }
        incomingBuffer = new byte[Settings.tcp.readBufferSize * 4];

        //UnityEngine.Debug.Log("[TCP] Attempting to connect to a server...");
        ConnectToServer(); // Connect to the RPI TCP server
    }


    public bool ConfigureIPmode(bool setStatic)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(Settings.tcp.adapterName) ||
           (setStatic && (string.IsNullOrWhiteSpace(Settings.tcp.ipAddress) || string.IsNullOrWhiteSpace(Settings.tcp.subnetMask))))
        {
            UnityEngine.Debug.LogError("[TCP] ConfigureIPmode: missing adapter/IP/mask.");
            return false;
        }

        // Prefer explicit full path to avoid WOW64 redirection shenanigans
        string netshPath = string.IsNullOrWhiteSpace(Settings.tcp.netshFileName)
            ? Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\netsh.exe")
            : Settings.tcp.netshFileName;

        // If running as a 32-bit process on 64-bit Windows, System32 is redirected; use Sysnative to reach real System32
        if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
        {
            string sysnative = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Sysnative\netsh.exe");
            if (File.Exists(sysnative)) netshPath = sysnative;
        }

        // Helper to run netsh and capture output
        bool RunNetsh(string arguments, out int exitCode, out string stdout, out string stderr)
        {
            var psi = new ProcessStartInfo
            {
                FileName = netshPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                stdout = p.StandardOutput.ReadToEnd();
                stderr = p.StandardError.ReadToEnd();
                bool exited = p.WaitForExit(Settings.tcp.IPsetTimeout);
                exitCode = exited ? p.ExitCode : -999;
                return exited;
            }
        }

        // 1) Confirm the interface exists and get its exact alias
        {
            RunNetsh("interface ipv4 show interfaces", out var code, out var so, out var se);
            // Optional Log
            //UnityEngine.Debug.Log($"[TCP] netsh show interfaces (exit {code})\n{so}\n{se}");
            // Quick presence check
            if (!so.Contains(Settings.tcp.adapterName))
            {
                UnityEngine.Debug.LogError($"[TCP] Adapter '{Settings.tcp.adapterName}' not found. " +
                                           "Use the exact alias from 'netsh interface ipv4 show interfaces'.");
                return false;
            }
        }

        // 2) Apply configuration
        string args = setStatic
            ? $"interface ipv4 set address name=\"{Settings.tcp.adapterName}\" source=static address={Settings.tcp.ipAddress} mask={Settings.tcp.subnetMask} gateway=none"
            : $"interface ipv4 set address name=\"{Settings.tcp.adapterName}\" source=dhcp";

        if (!RunNetsh(args, out var setCode, out var setOut, out var setErr))
        {
            UnityEngine.Debug.LogError("[TCP] Netsh did not exit within timeout.");
            return false;
        }
        if (setCode != 0)
        {
            UnityEngine.Debug.LogError($"[TCP] Netsh failed (exit {setCode}). Args: {args}\nOUT:\n{setOut}\nERR:\n{setErr}");
            return false;
        }

        // 3) Verify result (optional)
        //RunNetsh($"interface ipv4 show config name=\"{Settings.TCP.adapterName}\"", out var verCode, out var verOut, out var verErr);
        //UnityEngine.Debug.Log($"[TCP] Verify config (exit {verCode}):\n{verOut}\n{verErr}");

        UnityEngine.Debug.Log("[TCP] " + (setStatic ? "Static IP set." : "IP reset to DHCP."));
        return true;
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
                //UnityEngine.Debug.Log("[TCP] Connecting to Raspberry Pi at " + Settings.tcp.raspberryPiIP + ":" + Settings.tcp.port);
                client.Connect(Settings.tcp.raspberryPiIP, Settings.tcp.port);
            }
            else
            {
                // Connect to the local IP address for serial communication
                client.Connect(Settings.tcp.localIP, Settings.tcp.port);
            }

            // Get the network stream for reading and writing data
            stream = client.GetStream();

            // Set timeout for blocking reads
            stream.ReadTimeout = Settings.tcp.readTimeout;
            isConnected = true;

            if (Configuration.currentVersion == VRMode.Testbed)
                UnityEngine.Debug.Log("[TCP] Connected to Raspberry Pi at " + Settings.tcp.raspberryPiIP + ":" + Settings.tcp.port);
            else
                UnityEngine.Debug.Log("[TCP] Connected to local server at " + Settings.tcp.localIP + ":" + Settings.tcp.port);

            // Start the receive thread to listen for incoming messages
            receiveThread = new Thread(ReceiveViaTCP) { IsBackground = true, Name = "TCP.Receive" };
            receiveThread.Start();

            if (networkManager != null)
                // Send initial configuration to the RPI
                networkManager.SendTCPConfig();
            else
                UnityEngine.Debug.LogError("[TCP] NetworkManager reference is null, cannot send config over TCP.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("[TCP] Connecting to to TCP server failed for " + (Configuration.currentVersion == VRMode.Testbed ? "Raspberry Pi" : "local server") + ": " + e.Message);
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
        catch (Exception e) { UnityEngine.Debug.LogWarning("[TCP] Stream close failed: " + e.Message); }

        try
        {
            client?.Close();
            client?.Dispose();
        }
        catch (Exception e) { UnityEngine.Debug.LogWarning("[TCP] Client close failed: " + e.Message); }

        receiveThread = null;
        stream = null;
        client = null;

        if (Configuration.currentVersion == VRMode.Testbed)
            UnityEngine.Debug.Log("[TCP] Disconnected from Raspberry Pi at " + Settings.tcp.raspberryPiIP + ":" + Settings.tcp.port);
        else
            UnityEngine.Debug.Log("[TCP] Disconnected from local server at " + Settings.tcp.localIP + ":" + Settings.tcp.port);
    }


    public void SendViaTCP(object message, MessageType messageType)
    {
        // This method sends a message to the TCP server.

        // Check if the client is connected and the stream is not null
        if (!isConnected || stream == null)
        {
            UnityEngine.Debug.LogWarning("[TCP] Can not send a message, not connected to server.");
            return;
        }

        try
        {
            // Convert the message to bytes and send it over the stream
            byte[] byteMessage = message as byte[];
            byte[] data = EncodeTCPStream(byteMessage, messageType);
            stream.Write(data, 0, data.Length);

        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("[TCP] Send failed: " + e.Message);
            if (sendRetryCount > Settings.tcp.maxSendRetries)
            {
                sendRetryCount = 0;
                UnityEngine.Debug.LogError("[TCP] Multiple send failures, disconnecting.");
                isConnected = false; // Assume connection is lost
                return;
            }
            else
            {
                sendRetryCount++;
                SendViaTCP(message, messageType); // Retry sending the message
                UnityEngine.Debug.LogWarning($"[TCP] Send retry {sendRetryCount}/3");
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
        byte[] buffer = new byte[Settings.tcp.readBufferSize];

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

                    // Timeout is delivered as IOException with an inner SocketException = TimedOut on most runtimes.
                    var se = ex.InnerException as SocketException;
                    if (se != null && se.SocketErrorCode == SocketError.TimedOut)
                    {
                        // just idle: do NOT warn-spam on normal timeouts
                        continue;
                    }

                    // For other IO errors, log once and consider breaking
                    UnityEngine.Debug.LogWarning("[TCP] Read IO error: " + ex.Message);
                }
            }
        }
        catch (Exception e)
        {
            // Prevent errors during shutdown
            if (!isShuttingDown)
                UnityEngine.Debug.LogError("[TCP] Receive error: " + e.Message);
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
            if (payloadLength <= 0 || payloadLength > Settings.tcp.maxPacketSize)
            {
                UnityEngine.Debug.LogError($"[TCP] Invalid payload length: {payloadLength}, clearing buffer.");
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
                    UnityEngine.Debug.LogWarning("[TCP] MessageType " + packetType + " not found.");
                    return;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[TCP] MessageType parse error: " + e.Message);
                return;
            }

            // UnityEngine.Debug.Log("[TCP] Passing message type: " + msgType + " to CommRouter.");
            // Dispatch
            RouteQueueContainer.routeQueue.Add((payload, msgType));

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

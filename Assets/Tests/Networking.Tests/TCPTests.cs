using Contracts;
using UnityEngine;
using NUnit.Framework;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using System.Threading;

public class TCPTests
{
    NetworkManager networkManager;
    IConfigManagerConnector configManager;
    IMainThreadQueue MainThreadQueue;

    TCP tcp;

    [Test]
    public void InitTest()
    {
        // Test initialization of TCP class
        tcp = CreateTCP(true);

        MainThreadQueue = GetPrivateField<DummyMainThreadQueue>(tcp, "_IMainThreadQueue");
        configManager = GetPrivateField<DummyConfigManager>(tcp, "_IConfigManager");
        networkManager = GetPrivateField<NetworkManager>(tcp, "networkManager");

        bool isTestbed = GetPrivateField<bool>(tcp, "isTestbed");

        Assert.IsTrue(isTestbed, "TCP should be initialized in testbed mode.");

        Assert.IsNotNull(MainThreadQueue, "MainThreadQueue field should not be null after initialization.");
        Assert.IsNotNull(configManager, "ConfigManager field should not be null after initialization.");
        Assert.IsNotNull(networkManager, "NetworkManager field should not be null after initialization.");
    }


    [Test]
    public void TCPipConfigTest()
    {
        // Test initialization of TCP class
        tcp = CreateTCP(true);

        tcp.ConfigureIPmode(true);

        bool isDHCPEnabled = IsDhcpEnabled("Ethernet");
        Assert.IsTrue(!isDHCPEnabled, "Static IP is not enabled for the Ethernet adapter.");

        tcp.ConfigureIPmode(false);

        isDHCPEnabled = IsDhcpEnabled("Ethernet");
        Assert.IsTrue(isDHCPEnabled, "DHCP is not enabled for the Ethernet adapter.");
    }



    public static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field.GetValue(obj);
    }


    public TCP CreateTCP(bool isTestbed = true)
    {
        MainThreadQueue = new DummyMainThreadQueue();
        configManager = new DummyConfigManager();
        var go = new GameObject("TestNetworkManager");
        var netMgr = go.AddComponent<NetworkManager>();
        networkManager = netMgr;

        tcp = new TCP(networkManager, configManager, MainThreadQueue, isTestbed);

        return tcp;
    }


    public static bool IsDhcpEnabled(string adapterName)
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var adapter in adapters)
        {
            if (adapter.Name == adapterName || adapter.Description.Contains(adapterName))
            {
                var ipProps = adapter.GetIPProperties();
                var dhcp = ipProps.GetIPv4Properties()?.IsDhcpEnabled;
                return dhcp ?? false;
            }
        }

        throw new System.Exception($"Adapter {adapterName} not found.");
    }
}


public class DummyConfigManager : IConfigManagerConnector
{

    public void BindModule(object handler, string moduleName)
    {
        // Cast to the actual type
        TCP tcp = handler as TCP;
        if (tcp == null)
        {
            Debug.LogError("Handler is not of type TCP");
            return;
        }

        // Dummy implementation for testing
        tcp.ipAddress = "192.168.2.1";
        tcp.raspberryPiIP = "192.168.2.2";
        tcp.localIP = "127.0.0.1";
        tcp.subnetMask = "255.255.255.0";
        tcp.adapterName = "Ethernet";
        tcp.port = 65432;
        tcp.readBufferSize = 1024; // Size of the buffer for incoming data
        tcp.IPsetTimeout = 15000; // Timeout in miliseconds for IP configuration
        tcp.readTimeout = 2000; // Timeout in milliseconds for blocking reads
        tcp.maxPacketSize = 16777216; // Maximum packet size in bytes (16 MB)
        tcp.maxSendRetries = 3; // Maximum number of send retries
    }

    public VRMode GetVRType()
    {
        return VRMode.Testbed; // Dummy return value for testing
    }
}


public class DummyMainThreadQueue : IMainThreadQueue
{
    public void Enqueue(System.Action a)
    {
        // Dummy implementation for testing
        return;
    }

}

public class FakeNetworkManager : NetworkManager
{
    public new void SendTCPConfig() { return; }
    public new void RedirectMessage(int packetType, byte[] payload) { return; }
}

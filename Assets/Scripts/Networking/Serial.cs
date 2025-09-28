using UnityEngine;
using Contracts;


public class Serial : IModuleSettingsHandler
{
    private NetworkManager networkManager;
    private CommRouter commRouter;

    public Serial(NetworkManager networkManager)
    {
        this.networkManager = networkManager;
    }

    public void SettingsChanged(string moduleName)
    {
        // This method is called when settings are changed in the ConfigManager.
        // You can implement any necessary actions to handle the updated settings here.
    }

    public void Shutdown()
    {
        // Implement shutdown logic for the serial connection
        Debug.Log("Serial connection shutdown.");
    }

    public void InjectHardwareRouter(CommRouter router)
    {
        // This method injects the CommRouter instance into the TCP module.
        commRouter = router;
    }

    public void SendViaSerial(object message, MessageType type)
    {
        // Implement sending logic for the serial connection
        Debug.Log($"Sending via Serial: {message}");
    }
}

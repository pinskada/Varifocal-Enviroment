using UnityEngine;

public class Serial
{
    private NetworkManager networkManager;
    private CommRouter commRouter;

    public Serial(NetworkManager networkManager)
    {
        this.networkManager = networkManager;
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

    public void SendViaSerial(string message)
    {
        // Implement sending logic for the serial connection
        Debug.Log($"Sending via Serial: {message}");
    }
}

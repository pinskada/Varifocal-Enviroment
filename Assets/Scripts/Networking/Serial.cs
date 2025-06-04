using UnityEngine;

public class Serial
{
    private NetworkManager networkManager;

    public Serial(NetworkManager networkManager)
    {
        this.networkManager = networkManager;
    }

    public void Shutdown()
    {
        // Implement shutdown logic for the serial connection
        Debug.Log("Serial connection shutdown.");
    }
}

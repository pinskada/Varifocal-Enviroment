using UnityEngine;


[DisallowMultipleComponent]
public class Bootstrapper : MonoBehaviour
{
    public NetworkManager networkManager; // receives raw IMU JSON and produces IMUData
    public IMUHandler imuHandler; // filters IMUData and computes orientation
    public CameraHub cameraHub; // applies orientation to camera frustrums
    public ConfigManager configManager; // configuration manager for runtime parameters
    public GuiHub guiHub; // GUI manager for displaying information

    void Awake()
    {
        // Prevent duplicate bootstrappers when scenes reload
        if (Object.FindObjectsByType<Bootstrapper>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // Wire NetworkManager → IMUHandler (filter pipeline)
        networkManager.InjectModules(imuHandler);


        // Wire IMUHandler → OrientationApplier (apply filtered orientation)
        // Wire IMUHandler → ConfigManager (for runtime parameters)
        imuHandler.InjectModules(cameraHub, configManager);
    }
}

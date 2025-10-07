using UnityEngine;
using Contracts;

[DisallowMultipleComponent]
public class Bootstrapper : MonoBehaviour
{
    public NetworkManager networkManager; // receives raw IMU JSON and produces IMUData
    public IMUHandler imuHandler; // filters IMUData and computes orientation
    public CameraHub cameraHub; // applies orientation to camera frustrums
    public ConfigManager configManager; // configuration manager for runtime parameters
    public GuiInterface guiInterface; // GUI manager for displaying information
    public TCP tcp;
    public Serial serial; // Serial communication module (for UserVR mode)
    public VRSceneManager VRSceneManager; // Manages VR scene transitions

    void Awake()
    {
        // Prevent duplicate bootstrappers when scenes reload
        if (FindObjectsByType<Bootstrapper>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        (tcp, serial) = networkManager.GetCommunicatorInstance();

        Settings.Provider = configManager;

        // Local modules
        configManager.BindModule(imuHandler, "IMUHandler");
        configManager.BindModule(cameraHub, "Display");

        configManager.BindModule(networkManager, "NetworkManager");
        if (Configuration.currentVersion == VRMode.Testbed && tcp != null)
        {
            configManager.BindModule(tcp, "TCP");
        }
        if (Configuration.currentVersion == VRMode.UserVR && serial != null && tcp != null)
        {
            configManager.BindModule(serial, "Serial");
            configManager.BindModule(tcp, "TCP");
        }

        // External modules
        configManager.BindModule(networkManager, "Tracker");
        configManager.BindModule(networkManager, "Gaze");
        configManager.BindModule(networkManager, "Camera");
        configManager.BindModule(networkManager, "LeftCrop");
        configManager.BindModule(networkManager, "RightCrop");



        imuHandler.InjectModules(cameraHub, configManager);

        guiInterface.InjectModules(configManager, VRSceneManager);
    }
}

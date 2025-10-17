using UnityEngine;
using Contracts;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Bootstrapper : MonoBehaviour
{
    public NetworkManager networkManager; // receives raw IMU JSON and produces IMUData
    public CommRouter commRouter; // routes messages between modules
    public IMUHandler imuHandler; // filters IMUData and computes orientation
    public CameraFrustrum leftCameraFrustrum; // applies orientation to camera frustrums
    public CameraFrustrum rightCameraFrustrum;
    public CameraAligner cameraAligner; // interface to apply orientation to the camera
    public ConfigManager configManager; // configuration manager for runtime parameters
    public VRSceneManager VRSceneManager; // Manages VR scene transitions

    private TCP tcp;
    private Serial serial; // Serial communication module (for UserVR mode)
    private Madgwick filter; // Magdwick filter reference
    private GuiInterface guiInterface; // GUI manager for displaying information


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
        configManager.BindModule(leftCameraFrustrum, "display");
        configManager.BindModule(rightCameraFrustrum, "display");

        if (Configuration.currentVersion == VRMode.Testbed && tcp != null)
        {
            configManager.BindModule(tcp, "tcp");
        }
        if (Configuration.currentVersion == VRMode.UserVR && serial != null && tcp != null)
        {
            configManager.BindModule(serial, "serial");
            configManager.BindModule(tcp, "tcp");
        }

        // External modules
        configManager.BindModule(networkManager, "tracker");
        configManager.BindModule(networkManager, "gaze");
        configManager.BindModule(networkManager, "camera");
        configManager.BindModule(networkManager, "cameraCrop");

        imuHandler.InjectModules(cameraAligner);
        networkManager.InjectModules(commRouter);
    }

    void Start()
    {
        GetGuiComponents();
        filter = imuHandler.GetFilterInstance();

        guiInterface.InjectModules(configManager, VRSceneManager);
        configManager.BindModule(filter, "imu");

    }

    private void GetGuiComponents()
    {
        var uiSceneName = "UI_EditorScene";
        // Get the already-loaded UI scene
        var uiScene = SceneManager.GetSceneByName(uiSceneName);
        if (!uiScene.isLoaded)
        {
            Debug.LogError($"[Bootstrapper] Scene '{uiSceneName}' is not loaded. Load it additively before the Core scene.");
            return;
        }

        // Find exactly one GuiInterface in that scene
        guiInterface = FindInScene<GuiInterface>(uiScene);

        if (guiInterface == null)
        {
            Debug.LogError($"[Bootstrapper] No {nameof(GuiInterface)} found in '{uiSceneName}'.");
            return;
        }
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var hit = root.GetComponentInChildren<T>(true);
            if (hit == null) continue;

            if (found != null)
            {
                Debug.LogError($"[Bootstrapper] Multiple {typeof(T).Name} found in scene '{scene.name}'. Keep exactly one.");
                return null;
            }
            found = hit;
        }
        return found;
    }
}

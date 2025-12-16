using UnityEngine;
using Contracts;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Bootstrapper : MonoBehaviour
{
    public NetworkManager networkManager; // receives raw IMU JSON and produces IMUData
    public CommRouter commRouter; // routes messages between modules
    public IMUHandler imuHandler; // filters IMUData and computes orientation
    public GazeDistanceCalculator gazeDistanceCalculator; // calculates gaze distance from eye data
    public CameraFrustrum leftCameraFrustrum; // applies orientation to camera frustrums
    public CameraFrustrum rightCameraFrustrum;
    public CameraAligner cameraAligner; // interface to apply orientation to the camera
    public StereoDistortionComposite stereoDistortionComposite; // handles stereo distortion correction
    public ConfigManager configManager; // configuration manager for runtime parameters
    public VRSceneManager VRSceneManager; // Manages VR scene transitions

    private TCP tcp;
    private Serial serial; // Serial communication module (for UserVR mode)
    private Madgwick filter; // Magdwick filter reference
    private GuiInterface guiInterface; // GUI manager for displaying information
    private ImageRenderer imageRenderer; // GUI renderer for visual output


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
        configManager.BindModule(stereoDistortionComposite, "display");

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
        configManager.BindModule(networkManager, "eyeloop");
        configManager.BindModule(networkManager, "gaze2");
        configManager.BindModule(networkManager, "camera");
        configManager.BindModule(networkManager, "tracker_crop");

        imuHandler.InjectModules(cameraAligner);
        networkManager.InjectModules(commRouter);
    }

    void Start()
    {
        GetExternalComponents();
        filter = imuHandler.GetFilterInstance();

        guiInterface.InjectModules(configManager, VRSceneManager, imageRenderer, cameraAligner);
        configManager.BindModule(filter, "imu");
        configManager.BindModule(gazeDistanceCalculator, "gazeCalculator");


    }

    private void GetExternalComponents()
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
        imageRenderer = FindInScene<ImageRenderer>(uiScene);

        if (guiInterface == null)
        {
            Debug.LogError($"[Bootstrapper] No {nameof(GuiInterface)} found in '{uiSceneName}'.");
            return;
        }
        if (imageRenderer == null)
        {
            Debug.LogError($"[Bootstrapper] No {nameof(ImageRenderer)} found in '{uiSceneName}'.");
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

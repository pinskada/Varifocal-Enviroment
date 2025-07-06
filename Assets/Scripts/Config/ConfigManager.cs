using System;
using System.IO;
using UnityEngine;

public enum VRMode { Testbed, UserVR }

/// <summary>
/// MonoBehaviour-based manager that loads config at startup
/// and dispatches settings when components signal readiness.
/// Attach this to a GameObject in your initial scene.
/// </summary>
public class ConfigManager : MonoBehaviour
{
    // Singleton instance
    public static ConfigManager Instance { get; private set; }

    [Header("Mode Selection")]
    [SerializeField]
    private VRMode mode = VRMode.Testbed;

    public VRMode Mode => mode;
    public TestbedConfig TestbedConfig { get; private set; }
    public UserVRConfig UserVRConfig   { get; private set; }

    private string _configPath;

    // Events for subscribers to receive their config blocks
    public static event Action<DisplaySettings> OnDisplaySettingsLoaded;
    public static event Action<IMUSettings>     OnIMUSettingsLoaded;
    public static event Action<RpiConfig>       OnRpiConfigLoaded;
    public static event Action<Esp32Config>     OnEspConfigLoaded;
    public static event Action<EyeLoopConfig>   OnEyeLoopConfigLoaded;

    void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to component-ready events
        DisplayManager.OnReady   += InvokeDisplaySettings;
        IMUManager.OnReady       += InvokeIMUSettings;
        RpiManager.OnReady       += InvokeRpiConfig;
        Esp32Manager.OnReady     += InvokeEspConfig;
        EyeLoopManager.OnReady   += InvokeEyeLoopConfig;
    }

    void Start()
    {
        // Load JSON config once at startup
        LoadConfig();
    }

    /// <summary>
    /// Reads the JSON file for the chosen mode (Testbed or UserVR).
    /// Creates defaults if none exists, and saves them.
    /// </summary>
    private void LoadConfig()
    {
        string fileName = (mode == VRMode.Testbed)
            ? "testbedConfig.json"
            : "userVRConfig.json";
        _configPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(_configPath)) {
            string json = File.ReadAllText(_configPath);
            if (mode == VRMode.Testbed)
                TestbedConfig = JsonUtility.FromJson<TestbedConfig>(json);
            else
                UserVRConfig   = JsonUtility.FromJson<UserVRConfig>(json);
            Debug.Log($"[ConfigManager] Loaded config: {_configPath}");
        }
        else {
            // No config on disk → create defaults and persist
            if (mode == VRMode.Testbed)
                TestbedConfig = new TestbedConfig();
            else
                UserVRConfig   = new UserVRConfig();

            SaveConfig();
            Debug.LogWarning($"[ConfigManager] No config found; created default at {_configPath}");
        }
    }

    /// <summary>
    /// Serializes the active config back to JSON on disk.
    /// </summary>
    public void SaveConfig()
    {
        if (string.IsNullOrEmpty(_configPath))
            throw new InvalidOperationException("ConfigManager: LoadConfig() must be called before SaveConfig().");

        string json = (mode == VRMode.Testbed)
            ? JsonUtility.ToJson(TestbedConfig, prettyPrint: true)
            : JsonUtility.ToJson(UserVRConfig,  prettyPrint: true);
        File.WriteAllText(_configPath, json);
        Debug.Log($"[ConfigManager] Saved config: {_configPath}");
    }

    // These methods are called by component-ready events
    private void InvokeDisplaySettings()
    {
        var cfg = (mode == VRMode.Testbed)
            ? TestbedConfig.displaySettings
            : UserVRConfig.displaySettings;
        OnDisplaySettingsLoaded?.Invoke(cfg);
    }

    private void InvokeIMUSettings()
    {
        var cfg = (mode == VRMode.Testbed)
            ? TestbedConfig.imuSettings
            : UserVRConfig.imuSettings;
        OnIMUSettingsLoaded?.Invoke(cfg);
    }

    private void InvokeRpiConfig()
    {
        if (mode == VRMode.Testbed)
            OnRpiConfigLoaded?.Invoke(TestbedConfig.rpi);
    }

    private void InvokeEspConfig()
    {
        if (mode == VRMode.UserVR)
            OnEspConfigLoaded?.Invoke(UserVRConfig.esp);
    }

    private void InvokeEyeLoopConfig()
    {
        if (mode == VRMode.UserVR)
            OnEyeLoopConfigLoaded?.Invoke(UserVRConfig.eyeTracker);
    }
}

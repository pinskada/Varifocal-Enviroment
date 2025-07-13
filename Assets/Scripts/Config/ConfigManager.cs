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


    void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Load JSON config once at startup
        LoadConfig();
    }

    private void LoadConfig()
    {
        // Reads the JSON file for the chosen mode (Testbed or UserVR).
        // Creates defaults if none exists, and saves them.

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

    public void SaveConfig()
    {
        // Serializes the active config back to JSON on disk.

        if (string.IsNullOrEmpty(_configPath))
            throw new InvalidOperationException("ConfigManager: LoadConfig() must be called before SaveConfig().");

        string json = (mode == VRMode.Testbed)
            ? JsonUtility.ToJson(TestbedConfig, prettyPrint: true)
            : JsonUtility.ToJson(UserVRConfig,  prettyPrint: true);
        File.WriteAllText(_configPath, json);
        Debug.Log($"[ConfigManager] Saved config: {_configPath}");
    }

    public void getIMUSettings(out IMUSettings settings)
    {
        if (mode == VRMode.Testbed) {
            settings = TestbedConfig.imuSettings;
        } else {
            settings = UserVRConfig.imuSettings;
        }
    }
}

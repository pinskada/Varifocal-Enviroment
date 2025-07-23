using System;
using System.IO;
using UnityEngine;
using Contracts;

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
    public UserVRConfig UserVRConfig { get; private set; }
    private string _configPath;


    void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this)
        {
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

    void OnDestroy()
    {
        // 1. Save config
        // 2. Unregister all listeners
    }

    private void LoadConfig()
    {
        // Reads the JSON file for the chosen mode (Testbed or UserVR).
        // Creates defaults if none exists, and saves them.

        string fileName = (mode == VRMode.Testbed)
            ? "testbedConfig.json"
            : "userVRConfig.json";
        _configPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(_configPath))
        {
            string json = File.ReadAllText(_configPath);
            if (mode == VRMode.Testbed)
                TestbedConfig = JsonUtility.FromJson<TestbedConfig>(json);
            else
                UserVRConfig = JsonUtility.FromJson<UserVRConfig>(json);
            Debug.Log($"[ConfigManager] Loaded config: {_configPath}");
        }
        else
        {
            // No config on disk → create defaults and persist
            if (mode == VRMode.Testbed)
                TestbedConfig = new TestbedConfig();
            else
                UserVRConfig = new UserVRConfig();

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
            : JsonUtility.ToJson(UserVRConfig, prettyPrint: true);
        File.WriteAllText(_configPath, json);
        Debug.Log($"[ConfigManager] Saved config: {_configPath}");
    }

    void ChangeSetting<T>(string key, T newValue)
    {
        // Change a setting in the current config
        // 1. Set the value - void SetFieldByKeyPath(string key, object newValue)
        // 2. Save the config - SaveConfig()
        // 3. Broadcast to the listeners - void Broadcast(string key, object newValue)
    }

    void BindSection(object module)
    {
        // Bind a module to the current config section
        // 1. Reads the module's section prefix
        // 2. Scans the module for public properties
        // 3. a) Compute the key path for each property - Some.Prefix + "." + X
        //    b) Writes the initial value in the module - GetValue<T>(key) and SetValue(prop, value)
        //    c) Subscribes to changes - RegisterListener(key, v => prop.SetValue(module, v))
    }

    void RegisterListener(string key, Action<object> handler)
    {
        // Register a listener for changes to a specific config key
        // Adds the handler to a dictionary - Dictionary<string, List<Action<object>>> _listeners;
    }

    void UnregisterListener(string key, Action<object> handler)
    {
        // Unregister a listener for changes to a specific config key
        // Removes the handler from the dictionary - Dictionary<string, List<Action<object>>> _listeners;
    }

    void Broadcast(string key, object newValue)
    {
        // Invoke all registered handlers for that key
        // Loops through the dictionary/list of handlers for the key
        // and calls each handler with the new value
        // foreach (var h in _listeners[key]) h(newValue);
    }

    void SetFieldByKeyPath(string key, object value)
    {
        // Set a field in the current config by key path
        // 1. Split the key by '.' to get the path
        // 2. Navigate through the config object using reflection
        // 3. Set the final property or field to the new value
    }

    T GetValue<T>(string key)
    {
        // Extract a single setting value by its dot-path and return it
        
        return default(T); // Placeholder, implement logic to get value by key
    }
}

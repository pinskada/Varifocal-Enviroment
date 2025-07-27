using System;
using System.IO;
using UnityEngine;
using Contracts;
using System.Collections.Generic;

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

    // Dictionary to hold listeners for config changes
    // 1. Arg. - string: config key (e.g., "TestbedConfig.FieldName")
    // 2. Arg. - list of handlers: List<Action<object>> for that key points to a lambda function
    private Dictionary<string, List<Action<object>>> _subscriptions
    = new Dictionary<string, List<Action<object>>>();

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
        SaveConfig();
        UnregisterAllListeners();
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

    private void SaveConfig()
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

    public void BindModule(object handler, string moduleName)
    {
        // Bind a handler to the required module and initilize handlers values.
        // 1. Compute the key path for each property in the module - key = moduleName + "." + X
        // 2. Writes the initial values in the handler - GetValue<T>(key) and SetValue(prop, value)
        // 3. Subscribes to changes - 
        // - RegisterListener(string key, newValue => propInfo.SetValue(moduleInstance, newValue))

        // Example:

        // Get the handler instance by name
        var handlerType = handler.GetType();

        // Returns a dictionary of key-value pairs.
        var initialPropsValuesDict = GetPropValueDictFromModule(moduleName); 

        // Iterate through the keys and values
        foreach (var (key, initValue) in initialPropsValuesDict)
        {
            // Split the key into module name and property name
            var (_, propertyName) = SplitKey(key);

            // Try to get the property info by key
            try 
            { 
                var singleProp = handlerType.GetProperty(propertyName);

                singleProp.SetValue(handler, initValue);

                // Register a listener for changes to this key
                RegisterListenerToProperty(key, newValue => singleProp.SetValue(handler, newValue));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to bind property '{propertyName}' in module '{moduleName}': {ex.Message}");
            }
        
        }
    }
    
    Dictionary<string, object> GetPropValueDictFromModule(string moduleName)
    {
        // Returns a dictionary of key-value pairs from a given module
        // 1. Find all properties in the module by name
        // 2. For each property, get its value using - GetPropertyValue(key)
        // 3. Create a dictionary of property-value pairs
        // 4. Return the dictionary

        return new Dictionary<string, object>(); // Placeholder, implement logic to get section values
    }

    T GetPropertyValue<T>(string key)
    {
        // Extract a single setting value by its dot-path and return it

        return default(T); // Placeholder, implement logic to get value by key
    }

    private void RegisterListenerToProperty(string key, Action<object> handler)
    {
        // Register a listener for changes to a specific config key
        // Adds the handler lambda function to a dictionary - 
        //      - Dictionary<string, List<Action<object>>> _subscriptions;

        // Example:

        // If this is the first listener for this key, make a new list
        if (!_subscriptions.TryGetValue(key, out var list))
        {
            list = new List<Action<object>>();
            _subscriptions[key] = list;
        }

        // Add the handler to the list
        list.Add(handler);
        
    }

    private void UnregisterAllListeners()
    {
        // Unregister all listeners for changes
        _subscriptions.Clear();
    }

    public void ChangeProperty<T>(string key, T newValue)
    {
        // Change a Property in the current config
        // 1. Set the value - void SetFieldByKeyPath(key, newValue)
        // 2. Save the config - SaveConfig()
        // 3. Broadcast to the listeners - void Broadcast(key, newValue)

        SetFieldByKeyPath(key, newValue);
        SaveConfig();
        Broadcast(key, newValue);
    }

    private void SetFieldByKeyPath(string key, object rawValue)
    {
        // Set a field in the current config by key path
        // 1. Split the key by '.' to get the path - SplitKey(key)
        // 2. Navigate through the config object using reflection
        // 3. Set the final property or field to the new value

        var (moduleName, fieldName) = SplitKey(key);
        object root = (mode == VRMode.Testbed) ? TestbedConfig : UserVRConfig;

        // Grab the module block as a field
        var moduleField = root.GetType().GetField(moduleName);
        if (moduleField == null) {
            Debug.LogError($"Module '{moduleName}' not found on config.");
            return;
        }
        var moduleObj = moduleField.GetValue(root);

        // Grab the target field
        var targetField = moduleObj.GetType().GetField(fieldName);
        if (targetField == null) {
            Debug.LogError($"Field '{fieldName}' not found on module '{moduleName}'.");
            return;
        }

        // Convert + set
        try {
            var converted = Convert.ChangeType(rawValue, targetField.FieldType);
            targetField.SetValue(moduleObj, converted);
        }
        catch (Exception ex) {
            Debug.LogError($"Failed to set '{fieldName}' on '{moduleName}': {ex.Message}");
        }
    }


    private (string moduleName, string propertyName) SplitKey(string key)
    {
        // Checks if the key has only one '.' character
        // Split the key by '.' to get the path
        // Returns an array of strings representing the path

        // Example:

        // Check if the key is valid and contains exactly one '.'
        if (string.IsNullOrEmpty(key) || key.Split('.').Length != 2)
            Debug.LogError($"[ConfigManager] Invalid key format: {key}");

        // Split the key into module name and property name
        var parts = key.Split(new[]{'.'});

        // Return the module name and property name
        return (parts[0], parts[1]);
    }

    private void Broadcast(string key, object newValue)
    {
        // Invoke all registered handlers for that key
        // Loops through the dictionary/list of handlers for the key
        // and calls each handler with the new value

        // Example:

        // Look up all subscribers
        if (_subscriptions.TryGetValue(key, out var handlers))
        {
            // Call each one, handing it the new value
            foreach (var h in handlers)
                h(newValue);
        }
        else
        {
            Debug.LogWarning($"No listeners registered for key: {key}");
        }
    }
}

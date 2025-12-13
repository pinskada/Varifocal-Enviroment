using System;
using System.IO;
using UnityEngine;
using Contracts;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

/// MonoBehaviour-based manager that loads config at startup
/// and dispatches settings when components signal readiness.
/// Attach this to a GameObject in your initial scene.
public class ConfigManager : MonoBehaviour, IConfigManagerCommunicator, IConfigProvider<BaseConfig>
{
    // Singleton instance
    public static ConfigManager Instance { get; private set; }

    // Setup for different VR modes
    [Header("Mode Selection")]
    [SerializeField] private VRMode mode = VRMode.Testbed;
    public VRMode Mode => mode;

    // Configurations for different VR modes
    public BaseConfig Config { get; private set; }
    private string configPath;
    private string configSubfolder = "Configs";
    private string headsetSubFolder;
    private string configFolder;
    private List<string> configFileNames;
    private Thread configThread;

    // Dictionary to hold listeners for config changes
    // 1. Arg. - string: config key (e.g., "TestbedConfig.FieldName")
    // 2. Arg. - list of handlers: List<Action<object>> for that key points to a lambda function
    private Dictionary<string, List<IModuleSettingsHandler>> _subscriptions
    = new Dictionary<string, List<IModuleSettingsHandler>>();



    public void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load config
        LoadConfig();
    }


    public void Start()
    {
        // Start the receive thread to listen for incoming messages
        configThread = new Thread(DequeueConfig) { IsBackground = true, Name = "ConfigRouter" };
        configThread.Start();
    }


    private void DequeueConfig()
    {
        // Dequeue the config if present
        foreach (var newConfig in ConfigQueueContainer.configQueue.GetConsumingEnumerable())
        {
            var (key, newValue) = newConfig;
            if (key == null || newValue == null)
            {
                Debug.LogError("[ConfigManager] Received null config key or data.");
                return;
            }

            ChangeProperty(key, newValue);
        }
    }

    public void OnApplicationQuit()
    {
        ConfigQueueContainer.configQueue.CompleteAdding();
        configThread?.Join(1000); // Wait for the thread to finish
        // SaveConfig();
        UnregisterAllListeners();
    }


    private void LoadConfig()
    {
        /* Loads the default configuration based on the selected VR mode.
         * Sets the path for JSON storage and if it JSON exists, it loads it.
         * Applies the loaded values to the current config object.
         */

        // Initialize the default config based on the selected mode
        switch (mode)
        {
            case VRMode.Testbed:
                Config = new TestbedConfig();
                headsetSubFolder = "Testbed";
                break;
            case VRMode.UserVR:
                Config = new UserVRConfig();
                headsetSubFolder = "UserVR";
                break;
            default:
                Debug.LogError($"[ConfigManager] Unsupported VR mode: {mode}");
                return;
        }

        // Combine the persistent data path with the subfolders
        configFolder = Path.Combine(Application.persistentDataPath, configSubfolder, headsetSubFolder);

        // Check if ...\Config folder exists, if not create it
        if (!Directory.Exists(Path.Combine(Application.persistentDataPath, configSubfolder)))
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, configSubfolder));

        // Check if ...\UserVR or ...\Testbed folder exist, if not create it
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        // Combine the persistent data path with the subfolder and base name
        configPath = Path.Combine(configFolder, "Default.json");

        // Load the config from disk if it exists, otherwise create defaults
        if (File.Exists(configPath))
        {
            // Read the JSON file and deserialize into the config object
            string json = File.ReadAllText(configPath);
            // Apply any differences from the JSON to the current config
            JsonUtility.FromJsonOverwrite(json, Config);
            Debug.Log($"[ConfigManager] Loaded config: {configPath}");
        }
        else
        {
            // If no JSON config exists, create it
            SaveConfig();
            Debug.Log($"[ConfigManager] No config found; created default at {configPath}");
        }

        //Debug.Log($"[ConfigManager] Config loaded successfully.");

        // Initialize the config file names list
        ListFileNames(configFolder);

        //Debug.Log($"[ConfigManager] Config file names listed successfully.");
    }


    private void SaveConfig()
    {
        // Serializes the active config back to JSON on disk.

        if (string.IsNullOrEmpty(configPath))
            Debug.LogError("[ConfigManager] ConfigPath is empty.");

        // Serialize the current config to JSON and write it to the new path
        string json = JsonUtility.ToJson(Config, prettyPrint: true);
        File.WriteAllText(configPath, json);

        Debug.Log($"[ConfigManager] Saved config: {configPath}");
    }


    public void CreateNewConfigProfile(string profileName)
    {
        // Creates a new config profile by copying the current config
        // and saving it with the specified profile name.

        if (string.IsNullOrEmpty(profileName))
        {
            Debug.LogWarning("[ConfigManager] Profile name cannot be null or empty.");
            return;
        }

        // Create a new config path with the profile name
        configPath = Path.Combine(configFolder, $"{profileName}.json");

        SaveConfig();

        // Initialize the config file names list
        ListFileNames(configFolder);

        Debug.Log($"[ConfigManager] Created new config profile: {configPath}");
    }


    public void ChangeCurrentProfile(string profileName)
    {
        // Changes the current config profile to the specified one.
        // Loads the JSON file and applies it to the current config object.

        if (string.IsNullOrEmpty(profileName))
        {
            Debug.LogWarning("[ConfigManager] Profile name cannot be null or empty.");
            return;
        }

        // Create a new config path with the profile name
        configPath = Path.Combine(configFolder, $"{profileName}");

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            JsonUtility.FromJsonOverwrite(json, Config);
            Debug.Log($"[ConfigManager] Changed to config profile: {configPath}");
        }
        else
        {
            Debug.Log($"[ConfigManager] Config profile not found: {configPath}");
        }
    }


    private void ListFileNames(string folderPath)
    {
        // Creates a list of file names in the specified folder.
        var result = new List<string>();

        // Check if the folder exists
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[ConfigManager] Folder does not exist: {folderPath}");
            return;
        }
        // Get all files in that folder (non‐recursive)
        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);

        foreach (var fullPath in files)
        {
            // Extract the file name
            var fileName = Path.GetFileName(fullPath);

            // Skip "Test.json" (case-insensitive)
            if (string.Equals(fileName, "Test.json", StringComparison.OrdinalIgnoreCase))
                continue;

            // Extract just the file name and add to the list
            result.Add(Path.GetFileName(fullPath));
        }

        configFileNames = result;
    }


    public List<string> GetConfigFileNames()
    {
        // Returns the list of config file names
        return configFileNames;
    }


    public VRMode GetVRType()
    {
        // Returns the current VR mode
        // This is used to determine which setup to load in each module.

        return mode;
    }


    public void BindModule(IModuleSettingsHandler handler, string moduleName)
    {
        // Bind a handler to the required module and initilize handlers values.
        // 1. Compute the key path for each property in the module - key = moduleName + "." + X
        // 2. Writes the initial values in the handler - GetValue<T>(key) and SetValue(prop, value)
        // 3. Subscribes to changes -
        // - RegisterListener(string key, newValue => propInfo.SetValue(moduleInstance, newValue))


        // Returns a dictionary of key-value pairs.
        var initialPropsValuesDict = GetPropValueDictFromModule(moduleName);

        // Iterate through the keys and values
        foreach (var (key, _) in initialPropsValuesDict)
        {
            // Try to get the property info by key
            try
            {
                // Register a listener for changes to this key
                RegisterListenerToProperty(key, handler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to bind '{moduleName}': {ex.Message}");
            }
        }
    }


    private Dictionary<string, object> GetPropValueDictFromModule(string moduleName)
    {
        // Returns a dictionary of key-value pairs from a given module
        // 1. Find all properties in the module by name
        // 2. For each property, get its value using - GetPropertyValue(key)
        // 3. Create a dictionary of property-value pairs
        // 4. Return the dictionary

        // Initialize a dictionary to hold key-value pairs
        var keyValuePairs = new Dictionary<string, object>();

        // Get the module object by name
        var moduleObj = GetModuleObject(moduleName);
        if (moduleObj == null)
            return keyValuePairs;

        // Get module type
        Type moduleType = moduleObj.GetType();

        // Iterate through all public instance fields in the module type
        foreach (var field in moduleType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            // Get the value and key of the field from the module object
            object val = field.GetValue(moduleObj);
            string fullKey = $"{moduleName}.{field.Name}";

            // Add the key-value pair to the dictionary
            keyValuePairs[fullKey] = val;
        }

        return keyValuePairs;
    }


    private void RegisterListenerToProperty(string key, IModuleSettingsHandler handler)
    {
        // Register a listener for changes to a specific config key
        // Adds the handler lambda function to a dictionary -
        //      - Dictionary<string, List<Action<object>>> _subscriptions;

        // If this is the first listener for this key, make a new list
        if (!_subscriptions.TryGetValue(key, out var list))
        {
            list = new List<IModuleSettingsHandler>();
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
        Broadcast(key);
    }


    private void SetFieldByKeyPath(string key, object rawValue)
    {
        // Set a field in the current config by key path
        // 1. Split the key by '.' to get the path - SplitKey(key)
        // 2. Navigate through the config object using reflection
        // 3. Set the final property or field to the new value

        // Split the key into module name and field name
        var (moduleName, fieldName) = SplitKey(key);

        // Get the module object by name
        var moduleObj = GetModuleObject(moduleName);

        // Grab the target field
        var targetField = moduleObj.GetType().GetField(fieldName);
        if (targetField == null)
        {
            Debug.LogError($"[ConfigManager] Field '{fieldName}' not found on module '{moduleName}'.");
            return;
        }

        // Determine the type of the target field
        var targetType = targetField.FieldType;

        // If the field is nullable, get the underlying type
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
            targetType = underlying;

        // Convert + set
        try
        {
            object valueToSet;

            if (targetType.IsAssignableFrom(rawValue.GetType()))
            {
                // exact or derived type: just use it
                valueToSet = rawValue;
            }
            else if (targetType.IsEnum)
            {
                // Convert enum
                valueToSet = Enum.Parse(targetType, rawValue.ToString(), ignoreCase: true);
            }
            else
            {
                // Convert other types
                valueToSet = Convert.ChangeType(rawValue, targetType);
            }

            // Set the value on the target field
            targetField.SetValue(moduleObj, valueToSet);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConfigManager] Failed to set '{fieldName}' on '{moduleName}': {ex.Message}");
        }
    }


    private (string moduleName, string fieldName) SplitKey(string key)
    {
        // Checks if the key has only one '.' character
        // Split the key by '.' to get the path
        // Returns an array of strings representing the path

        // Check if the key is null or whitespace
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogError("[ConfigManager] Key cannot be null or whitespace");
            return (null, null);
        }

        // Split the key by '.' to get the module name and property name
        var parts = key.Split(new[] { '.' }, StringSplitOptions.None);

        // Check if the key is valid and contains exactly one '.'
        if (parts.Length != 2
            || string.IsNullOrWhiteSpace(parts[0])
            || string.IsNullOrWhiteSpace(parts[1]))
        {
            Debug.LogError($"[ConfigManager] Key must be in the form 'ModuleName.fieldName': '{key}'");
            return (null, null);
        }

        // Return the module name and property name
        return (parts[0], parts[1]);
    }


    private void Broadcast(string key)
    {
        // Invoke all registered handlers for that key
        // Loops through the dictionary/list of handlers for the key
        // and calls each handler with the new value

        var (moduleName, fieldName) = SplitKey(key);

        // Look up all subscribers
        if (_subscriptions.TryGetValue(key, out var handlers))
        {
            // Call each one, handing it the new value
            foreach (var h in handlers)
            {
                if (h is IModuleSettingsHandler settingsHandler)
                {
                    settingsHandler.SettingsChanged(moduleName, fieldName);
                }
                else
                {
                    Debug.Log($"[ConfigManager] Handler {h.GetType().Name} does not implement SettingsChanged, skipping.");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ConfigManager] No listeners registered for key: {key}");
        }
    }


    private object GetModuleObject(string moduleName)
    {
        // Returns the module object by name
        // 1. Get the field from the config object
        // 2. Return the value of that field

        // Grab the module block as a field
        var moduleField = Config.GetType().GetField(moduleName);
        if (moduleField == null)
        {
            Debug.LogError($"[ConfigManager] Module '{moduleName}' not found on config.");
            return null;
        }
        var moduleObj = moduleField.GetValue(Config);

        return moduleObj;
    }
}

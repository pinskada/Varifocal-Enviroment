using UnityEngine;
using Contracts;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;


public class GuiInterface : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    [SerializeField] private TMP_Dropdown configSettingsDropdown;
    private IConfigManagerCommunicator _IConfigManager;


    public void InjectModules(IConfigManagerCommunicator _IConfigManager)
    {
        this._IConfigManager = _IConfigManager;
    }


    private void Start()
    {
        if (!Application.isPlaying) return;

        SetVersionText();
        ConnectEditFields();
        PopulateEditFields();
        PopulateSettingsConfigDropdown();

    }


    private void SetVersionText()
    {
        var versionText = GameObject.Find("VersionText")?.GetComponent<Text>();
        if (versionText != null)
        {
            var currentVersion = _IConfigManager.GetVRType();
            var currentIntVersion = (int)currentVersion;
            versionText.text = $"Version: {currentIntVersion}:  {currentVersion}";
        }
    }


    private void ConnectEditFields()
    {
        foreach (var input in FindObjectsByType<InputField>(FindObjectsSortMode.None))
        {
            input.onEndEdit.AddListener(value => OnFieldEdited(input, value));
        }
    }


    private void PopulateEditFields()
    {
        foreach (var input in FindObjectsByType<InputField>(FindObjectsSortMode.None))
        {
            var field = input.GetComponent<UIField>();

            if (field == null)
            {
                Debug.LogError($"[GUI] InputField {input.gameObject.name} missing UIField component");
                return;
            }

            string module = field.moduleName;
            string name = field.fieldName;

            if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"[GUI] InputField {input.gameObject.name} has empty module or field name");
                return;
            }

            // TODO: Get current config value from backend
            var value = GetSettingValue(module, name);
            if (value != null)
            {
                try
                {
                    input.text = value.ToString();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GUI] Failed to set InputField text: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[GUI] Config {module}.{name} not found");
            }
        }
    }


    private void PopulateSettingsConfigDropdown()
    {
        var configList = _IConfigManager.GetConfigFileNames();
        if (configList == null || configList.Count == 0) return;

        configSettingsDropdown.AddOptions(configList);
        configSettingsDropdown.options.Add(new TMP_Dropdown.OptionData("+ New Profile"));
    }


    private object GetSettingValue(string moduleName, string fieldName)
    {
        try
        {
            // Get the type of the Settings class
            var settingsType = typeof(Settings);

            // Find the field representing the module
            var moduleField = settingsType.GetField(moduleName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (moduleField == null)
            {
                Debug.LogWarning($"[Settings] Module '{moduleName}' not found in Settings");
                return null;
            }

            // Get the instance of that module
            var moduleInstance = moduleField.GetValue(null); // Static field → null instance

            // Find the field inside the module
            var fieldInfo = moduleInstance.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Debug.LogWarning($"[Settings] Field '{fieldName}' not found in module '{moduleName}'");
                return null;
            }

            // Extract and return the value
            return fieldInfo.GetValue(moduleInstance);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Settings] Failed to extract {moduleName}.{fieldName}: {ex.Message}");
            return null;
        }
    }


    public void OnConfigProfileChanged(int Index)
    {
        // Change config profile based on dropdown index
        var configList = _IConfigManager.GetConfigFileNames();
        if (configList == null || Index < 0 || Index > configList.Count)
        {
            Debug.LogWarning($"[GUI] Invalid config profile index: {Index}");
            return;
        }

        if (Index == configList.Count)
        {
            PromptForNewConfigProfile();
            return;
        }

        var selectedProfile = configList[Index];
        Debug.Log($"[GUI] Changing to config profile: {selectedProfile}");

        _IConfigManager.ChangeCurrentProfile(selectedProfile);

        PopulateEditFields();
    }


    private void PromptForNewConfigProfile()
    {
        Debug.Log("[GUI] Adding new profile...");
        // TODO: Add a small input popup here
        string newProfileName = "NewProfile_" + DateTime.Now.ToString("HHmmss");
        _IConfigManager.CreateNewConfigProfile(newProfileName);
        PopulateSettingsConfigDropdown();
    }


    public void OnModeChanged(int Index)
    {
        // GUI mode set based on dropdown index

        if (Index < 0 || Index >= panels.Count)
        {
            Debug.LogWarning($"[GUI] Invalid panel index: {Index}");
            return;
        }

        foreach (var panel in panels)
            panel.SetActive(false);

        panels[Index].SetActive(true);
    }


    // Called by input fields
    public void OnFieldEdited(InputField input, string value)
    {
        var field = input.GetComponent<UIField>();
        if (field == null) return;

        string module = field.moduleName;
        string name = field.fieldName;

        if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[GUI] Missing module or field name");
            return;
        }

        SendConfig(module, name, value);
    }


    // Called by buttons
    public void OnButtonPressed()
    {
        var field = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<UIField>();
        if (field == null) return;

        string module = field.moduleName;
        string name = field.fieldName;

        SendConfig(module, name, "1"); // or any fixed button value
    }


    private void SendConfig(string moduleName, string fieldName, string value)
    {
        var key = $"{moduleName}.{fieldName}";

        Debug.Log($"[GUI] {moduleName}.{fieldName} = {value}");

        // Example: generic backend call
        ConfigQueueContainer.configQueue.Add((key, value));
    }
}

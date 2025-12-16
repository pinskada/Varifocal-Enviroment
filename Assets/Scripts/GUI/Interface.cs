using UnityEngine;
using Contracts;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Reflection;


public class GuiInterface : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    [SerializeField] private TMP_Dropdown configSettingsDropdown;
    [SerializeField] private UICreateProfile createProfileHandler;

    public IConfigManagerCommunicator _IConfigManager;
    public ISceneManagement _VRSceneManager;
    public ImageDestroyer _imageDestroyer;
    public ICameraAligner _cameraAligner;

    public void InjectModules(
        IConfigManagerCommunicator _IConfigManager,
        ISceneManagement _VRSceneManager,
        ImageDestroyer _imageDestroyer,
        ICameraAligner _cameraAligner
    )
    {
        this._IConfigManager = _IConfigManager;
        this._VRSceneManager = _VRSceneManager;
        this._imageDestroyer = _imageDestroyer;
        this._cameraAligner = _cameraAligner;
    }

    private void Awake()
    {
        PrewireUiSections();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        SetVersionText();
        ConnectEditFields();
        ConnectToggles();
        PopulateEditFields();
        PopulateToggles();
        PopulateSettingsConfigDropdown();
    }


    void Update()
    {
        // Cycle to next scene on Right Arrow
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _VRSceneManager.NextScene();
        }

        // Cycle to previous scene on Left Arrow
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _VRSceneManager.PreviousScene();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _cameraAligner.PreviousTarget();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _cameraAligner.PreviousTarget();
        }
    }


    private void PrewireUiSections()
    {
        var sections = FindObjectsByType<UISection>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        foreach (var s in sections)
            s.ApplyToChildren();
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

    private IEnumerable<TMP_InputField> GetAllInputs()
    {
        // Unity 6 (6000.0.42f1) supports the includeInactive flag
        return FindObjectsByType<TMP_InputField>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }


    private void ConnectEditFields()
    {
        foreach (var input in GetAllInputs())
        {
            input.onEndEdit.AddListener(value => OnFieldEdited(input, value));
        }
    }

    private IEnumerable<Toggle> GetAllToggles()
    {
        return FindObjectsByType<Toggle>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }


    private void ConnectToggles()
    {
        foreach (var toggle in GetAllToggles())
        {
            toggle.onValueChanged.AddListener(isOn => OnToggleChanged(toggle, isOn));
        }
    }


    private void PopulateToggles()
    {
        foreach (var toggle in GetAllToggles())
        {
            var field = toggle.GetComponent<UIField>();
            if (field == null) continue;

            string module = field.moduleName;
            string name = field.fieldName;

            if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(name))
                continue;

            var value = GetSettingValue(module, name, null);
            if (value == null) continue;

            // Expecting bool in Settings
            if (value is bool b)
                toggle.SetIsOnWithoutNotify(b);
            else
                Debug.LogWarning($"[GuiInterface] Setting {module}.{name} is not bool (got {value.GetType().Name})");
        }
    }

    public void OnToggleChanged(Toggle toggle, bool isOn)
    {
        var field = toggle.GetComponent<UIField>();
        if (field == null) return;

        string module = field.moduleName;
        string name = field.fieldName;

        if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[GuiInterface] Missing module or field name on Toggle");
            return;
        }

        // Send as "true"/"false" (or swap to "1"/"0" if your backend expects that)
        SendConfig(module, name, isOn ? "true" : "false");
    }


    private void PopulateEditFields()
    {
        //Debug.Log("[GuiInterface] Populating edit fields");
        foreach (var input in GetAllInputs())
        {
            if (input.gameObject.name == "InputNameField") continue;

            //Debug.Log($"[GuiInterface] Found InputField: {input.gameObject.name}");
            var field = input.GetComponent<UIField>();

            if (field == null)
            {
                Debug.LogError($"[GuiInterface] InputField {input.gameObject.name} missing UIField component");
                continue;
            }


            string module = field.moduleName;
            string name = field.fieldName;

            if (string.IsNullOrEmpty(module))
            {
                Debug.LogError($"[GuiInterface] InputField {input.gameObject.name} has empty module name");
                continue;
            }
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"[GuiInterface] InputField {input.gameObject.name} has empty field name");
                continue;
            }

            // if (module == "gaze")
            // {
            //     continue;
            // }
            var value = GetSettingValue(module, name, input);
            if (value != null)
            {
                try
                {
                    //Debug.Log($"[GuiInterface] Setting {module}.{name} to {value}");
                    input.text = value.ToString();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GuiInterface] Failed to set InputField text: {ex.Message}");
                }
            }
        }
    }


    private void PopulateSettingsConfigDropdown()
    {
        var configList = _IConfigManager.GetConfigFileNames();
        if (configList == null || configList.Count == 0) return;
        configSettingsDropdown.ClearOptions();
        configSettingsDropdown.AddOptions(configList);
        configSettingsDropdown.options.Add(new TMP_Dropdown.OptionData("+ New Profile"));
    }


    private object GetSettingValue(string moduleName, string fieldName, Component source = null)
    {
        try
        {
            var settingsType = typeof(Settings);

            var moduleProp = settingsType.GetProperty(moduleName, BindingFlags.Public | BindingFlags.Static);
            if (moduleProp == null)
            {
                var srcName = source ? source.gameObject.name : "(unknown)";
                Debug.LogWarning($"[GuiInterface] Module '{moduleName}' of gameObject {srcName} not found in Settings");
                return null;
            }

            var moduleInstance = moduleProp.GetValue(null);
            if (moduleInstance == null)
            {
                Debug.LogWarning($"[GuiInterface] Module '{moduleName}' instance is null. Is Settings.Provider set?");
                return null;
            }

            var fieldInfo = moduleInstance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Debug.LogWarning($"[GuiInterface] Field '{fieldName}' not found in module '{moduleName}'");
                return null;
            }

            return fieldInfo.GetValue(moduleInstance);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GuiInterface] Failed to extract {moduleName}.{fieldName}: {ex.Message}");
            return null;
        }
    }


    public void OnSceneChanged(int Index)
    {
        // Scene set based on dropdown index
        configSettingsDropdown.SetValueWithoutNotify(-1);

        switch (Index)
        {
            case 0:
                _VRSceneManager.LoadCalibScene();
                break;
            case 1:
                _VRSceneManager.PreviousScene();
                break;
            case 2:
                _VRSceneManager.NextScene();
                break;
            default:
                Debug.LogWarning($"[GuiInterface] Invalid scene index: {Index}");
                break;
        }
    }


    public void OnModeChanged(int Index)
    {
        // RPI mode set based on dropdown index
        var action = "";
        switch (Index)
        {
            case 0:
                action = "no_preview";
                _imageDestroyer.ControlTextures(false);
                break;
            case 1:
                action = "camera_preview";
                _imageDestroyer.ControlTextures(true);
                break;
            case 2:
                action = "pupil_preview";
                _imageDestroyer.ControlTextures(true);
                break;
            case 3:
                action = "cr_preview";
                _imageDestroyer.ControlTextures(true);
                break;
        }
        var message = new Dictionary<string, string>
        {
            { "mode", action }
        };

        RouteQueueContainer.routeQueue.Add((message, MessageType.trackerControl));
    }


    public void OnConfigProfileChanged(int Index)
    {
        // Change config profile based on dropdown index
        var configList = _IConfigManager.GetConfigFileNames();
        if (configList == null || Index < 0 || Index > configList.Count)
        {
            Debug.LogWarning($"[GuiInterface] Invalid config profile index: {Index}");
            return;
        }

        if (Index == configList.Count)
        {
            Debug.Log("[GuiInterface] Prompting for new profile name");
            createProfileHandler.PromptUserForProfileName(OnProfileNameEntered);
            return;
        }

        var selectedProfile = configList[Index];
        Debug.Log($"[GuiInterface] Changing to config profile: {selectedProfile}");

        _IConfigManager.ChangeCurrentProfile(selectedProfile);

        PopulateEditFields();
        PopulateSettingsConfigDropdown();
    }


    private void OnProfileNameEntered(string newProfileName)
    {
        if (string.IsNullOrEmpty(newProfileName))
        {
            Debug.Log("[GuiInterface] New profile creation cancelled or invalid name");
            return;
        }

        _IConfigManager.CreateNewConfigProfile(newProfileName);
        PopulateSettingsConfigDropdown();
        PopulateEditFields();
    }


    public void OnPanelChanged(int Index)
    {
        // GUI mode set based on dropdown index

        if (Index < 0 || Index >= panels.Count)
        {
            Debug.LogWarning($"[GuiInterface] Invalid panel index: {Index}");
            return;
        }

        foreach (var panel in panels)
            panel.SetActive(false);

        panels[Index].SetActive(true);

        if (Index == panels.Count - 1)
            _VRSceneManager.LoadCalibScene();

        PopulateEditFields();
    }


    public void OnFieldEdited(TMP_InputField input, string value)
    {
        var field = input.GetComponent<UIField>();
        if (field == null) return;

        string module = field.moduleName;
        string name = field.fieldName;

        if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[GuiInterface] Missing module or field name");
            return;
        }

        if (module == "gazeCalculator" && name == "targetPreviewDistance")
        {
            TargetDistanceQueueContainer.TargetDistanceQueue.Enqueue(float.Parse(value));
            return;
        }

        SendConfig(module, name, value);
    }


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

        Debug.Log($"[GuiInterface] {moduleName}.{fieldName} = {value}");

        // Example: generic backend call
        ConfigQueueContainer.configQueue.Add((key, value));
    }
}

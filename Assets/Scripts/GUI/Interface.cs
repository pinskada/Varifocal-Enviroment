using UnityEngine;
using Contracts;
using System.Collections.Generic;
using UnityEngine.UI;

public class GuiInterface : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    private IConfigManagerCommunicator _IConfigManager;

    private void Awake()
    {
        // Find all InputFields in the scene and wire them
        foreach (var input in FindObjectsByType<InputField>(FindObjectsSortMode.None))
        {
            input.onEndEdit.AddListener(value => OnFieldEdited(input, value));
        }
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
        ConfigQueueContainer.configQueue.Enqueue((key, value));
    }
}

using UnityEngine;
using Contracts;
using System.Collections.Generic;

public class GuiInterface : MonoBehaviour
{
    public GuiHub guiHub;


    // Called by input fields
    public void OnFieldEdited(string value)
    {
        var field = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<UIField>();
        if (field == null) return;

        string module = field.moduleName;
        string name = field.fieldName;

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
        var payload = new Dictionary<string, object>
        {
            { $"{moduleName}.{fieldName}", value }
        };

        Debug.Log($"[GUI] {moduleName}.{fieldName} = {value}");

        // Example: generic backend call
        RouteQueueContainer.routeQueue.Enqueue((payload, MessageType.espConfig));
    }
}

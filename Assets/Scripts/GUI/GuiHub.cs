using UnityEngine;
using Contracts;
using System.Collections.Generic;

public class GuiHub : MonoBehaviour, IGUIHub, IModuleSettingsHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SettingsChanged(string moduleName, string fieldName)
    {
        // Implementation for handling settings changes in the GUI
    }

    public void pushConfigList(List<string> configFileNames)
    {
        // Implementation for pushing config file names to the GUI
    }

    public void SendConfigToRpi() { }

    public void SendConfigToEsp32() { }

    public void SendConfigToLocalEyeTracker() { }

    public void HandlePreviewImage(byte[] payload) { }
}

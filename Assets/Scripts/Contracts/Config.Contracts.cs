using System.Collections.Generic;

namespace Contracts
{
    public enum VRMode
    {
        Testbed,
        UserVR
    }

    // Contract for managing configuration settings.
    public interface IConfigManagerConnector
    {
        // Get current VR mode
        public VRMode GetVRType();

        // Bind a handler to a specific module
        public void BindModule(IModuleSettingsHandler handler, string moduleName);
    }

    public interface IConfigProvider<BaseConfig>
    {
        BaseConfig Config { get; }
    }

    public interface IConfigManagerCommunicator
    {
        // Apply the settings
        public void ChangeProperty<T>(string key, T newValue);

        // Change current configuration profile
        public void ChangeCurrentProfile(string profileName);

        // Create a new configuration profile
        public void CreateNewConfigProfile(string profileName);

        // Get the list of configuration file names
        public List<string> GetConfigFileNames();
    }

    public interface IModuleSettingsHandler
    {
        // Method to change settings based on updated configuration
        public void SettingsChanged(string moduleName, string fieldName);
    }
}

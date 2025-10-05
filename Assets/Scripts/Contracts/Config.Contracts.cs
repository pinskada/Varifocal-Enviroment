using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Contracts
{
    public enum VRMode
    {
        Testbed,
        UserVR
    }


    public interface IConfigProvider<BaseConfig>
    {
        BaseConfig Config { get; }
    }

    public interface IConfigManagerCommunicator
    {
        // Get current VR mode
        public VRMode GetVRType();

        // Bind a handler to a specific module
        public void BindModule(IModuleSettingsHandler handler, string moduleName);

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

    public static class ConfigQueueContainer
    {
        public static readonly ConcurrentQueue<(string key, object newValue)> configQueue =
            new ConcurrentQueue<(string key, object newValue)>();
    }
}

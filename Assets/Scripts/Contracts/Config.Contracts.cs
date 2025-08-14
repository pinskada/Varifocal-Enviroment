
namespace Contracts
{
    public enum VRMode { Testbed, UserVR }

    // Contract for managing configuration settings.
    public interface IConfigManagerConnector
    {
        // Get current VR mode
        public VRMode GetVRType();

        // Bind a handler to a specific module
        public void BindModule(object handler, string moduleName);
    }

    public interface IConfigManagerCommunicator
    {
        // Apply the settings
        public void ChangeProperty<T>(string key, T newValue);

        // Change current configuration profile
        public void ChangeCurrentProfile(string profileName);

        // Create a new configuration profile
        public void CreateNewConfigProfile(string profileName);
    }
}

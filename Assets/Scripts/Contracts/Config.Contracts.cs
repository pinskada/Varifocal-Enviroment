
namespace Contracts
{
    // Contract for managing configuration settings.
    public interface IConfigManager
    {
        // Apply the settings
        public void ChangeProperty<T>(string key, T newValue);

        // Bind a handler to a specific module
        public void BindModule(object handler, string moduleName);

        // Change current configuration profile
        public void ChangeCurrentProfile(string profileName);

        // Create a new configuration profile
        public void CreateNewConfigProfile(string profileName);
    }
}

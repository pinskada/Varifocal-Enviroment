using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using System.IO;
using Contracts;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.TestTools;

public class ConfigManagerTests
{
    private IConfigManagerCommunicator _IConfigManagerCommunicator;
    private IConfigManagerConnector _IConfigManagerConnector;


    [Test]
    public void TestInit()
    {
        // Create an instance of ConfigManager
        ConfigManager configManager = CreateConfigManager();

        // Initialize the ConfigManager
        configManager.Start();

        // Get the config path
        object ConfigPOCO = GetPrivateField<object>(configManager, "Config");

        Assert.IsNotNull(ConfigPOCO, "Config should not be null after initialization.");
    }


    [Test]
    public void TestJSONonPropChange()
    {
        // Create an instance of ConfigManager
        ConfigManager configManager = CreateConfigManager();

        // Start the ConfigManager to initialize it
        configManager.Start();

        // Get the initial config path
        string configPath = GetConfigPath(configManager, "Default");

        // Inject the IConfigManager interfaces
        _IConfigManagerCommunicator = configManager;
        _IConfigManagerConnector = configManager;

        // Get the current time
        var currentWriteTime = System.DateTime.Now;

        // Change properties in the config
        (int randomInt, float randomFloat, string randomString) = ChangeTestProperties(_IConfigManagerCommunicator);

        // Determine the last write time of the config file
        var lastWriteTime = File.GetLastWriteTime(configPath);

        // Ensure the config file was written to disk
        var timeDelta = (lastWriteTime - currentWriteTime).TotalSeconds;
        Assert.GreaterOrEqual(timeDelta, -1, $"Config file was not written to disk");

        // Read the JSON from the config file
        (int actualInt, float actualFloat, string actualString) = GetJson(configPath, configManager);

        Assert.AreEqual(randomInt, actualInt, "The integer value in the config file does not match the expected value.");
        Assert.AreEqual(randomFloat, actualFloat, "The float value in the config file does not match the expected value.");
        Assert.AreEqual(randomString, actualString, "The string value in the config file does not match the expected value.");
    }


    [Test]
    public void TestBinding()
    {
        // Create an instance of ConfigManager
        ConfigManager configManager = CreateConfigManager();

        // Start the ConfigManager to initialize it
        configManager.Start();

        // Inject the IConfigManager interfaces
        _IConfigManagerCommunicator = configManager;
        _IConfigManagerConnector = configManager;

        // Create a dummy handler
        DummyConfigProvider dummyHandler = new GameObject("DummyHandler").AddComponent<DummyConfigProvider>();

        // Bind the dummy handler to the ConfigManager
        dummyHandler.bindThisModule(_IConfigManagerConnector);

        // Get the initial config path
        string configPath = GetConfigPath(configManager, "Default");

        // Read the JSON from the config file
        (int actualInt, float actualFloat, string actualString) = GetJson(configPath, configManager);

        // Verify that the dummy handler's properties are set correctly
        Assert.AreNotEqual(0, dummyHandler.testInt, "Test integer was not initialsed.");
        Assert.AreNotEqual(0f, dummyHandler.testFloat, "Test float was not initialsed.");
        Assert.IsNotNull(dummyHandler.testString, "Test string was not initialsed.");

        // Verify that the dummy handler's properties match the expected values
        Assert.AreEqual(dummyHandler.testInt, actualInt, "The integer value in the config file does not match the dummy handler's value.");
        Assert.AreEqual(dummyHandler.testFloat, actualFloat, "The float value in the config file does not match the dummy handler's value.");
        Assert.AreEqual(dummyHandler.testString, actualString, "The string value in the config file does not match the dummy handler's value.");

        // Change properties in the config using the IConfigManager
        (int randomInt, float randomFloat, string randomString) = ChangeTestProperties(_IConfigManagerCommunicator);

        // Verify that the dummy handler's properties are updated correctly
        Assert.AreEqual(randomInt, dummyHandler.testInt, "The integer value in the dummy handler does not match the expected value after change.");
        Assert.AreEqual(randomFloat, dummyHandler.testFloat, "The float value in the dummy handler does not match the expected value after change.");
        Assert.AreEqual(randomString, dummyHandler.testString, "The string value in the dummy handler does not match the expected value after change.");
    }


    [Test]
    public void TestProfiles()
    {
        // Create an instance of ConfigManager
        ConfigManager configManager = CreateConfigManager();

        // Start the ConfigManager to initialize it
        configManager.Start();

        // Inject the IConfigManager interfaces
        _IConfigManagerCommunicator = configManager;
        _IConfigManagerConnector = configManager;

        // Create a new test profile
        _IConfigManagerCommunicator.CreateNewConfigProfile("Test");

        // Get path to test profile
        string configPath = GetConfigPath(configManager, "Test");

        // Get the current time
        var currentWriteTime = System.DateTime.Now;

        // Read the JSON from the config file
        GetJson(configPath, configManager);

        // Determine the last write time of the config file
        var lastWriteTime = File.GetLastWriteTime(configPath);

        // Ensure the test config file was written to disk
        var timeDelta = (lastWriteTime - currentWriteTime).TotalSeconds;
        Assert.GreaterOrEqual(timeDelta, -1, $"Test config file was not written to disk");

        // Change profile back to default
        _IConfigManagerCommunicator.ChangeCurrentProfile("Default");

        // Get path to test profile
        configPath = GetConfigPath(configManager, "Default");

        // Get the config path
        object actualPath = GetPrivateField<object>(configManager, "configPath");

        Assert.AreEqual(configPath, actualPath, "Config path should match the test profile path after changing profile.");
    }


    public ConfigManager CreateConfigManager()
    {
        // Create a dummy GameObject
        var go = new GameObject("TestObject");

        // Attach your MonoBehaviour (ConfigManager)
        var configManager = go.AddComponent<ConfigManager>();

        return configManager;
    }


    public static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field.GetValue(obj);
    }


    public string GetConfigPath(object configManager, string fileName)
    {
        // Get the VRMode field from the ConfigManager instance
        var mode = GetPrivateField<VRMode>(configManager, "mode");

        // Determine the file name based on the VR mode
        string headsetSubFolder = (mode == VRMode.Testbed)
        ? "Testbed"
        : "UserVR";

        // Construct the full path to the config file
        string _configPath = Path.Combine(Application.persistentDataPath, "Configs", headsetSubFolder, fileName + ".json");

        Debug.Log($"Config path: {_configPath}");

        return _configPath;
    }



    public (int, float, string) GetJson(string configPath, ConfigManager configManager)
    {
        string json;
        try
        {
            json = File.ReadAllText(configPath);
        }
        catch (IOException ex)
        {
            Assert.Fail($"Failed to read config file: {ex.Message}");
            return (0, 0f, null);
        }


        // Get the VRMode field from the ConfigManager instance
        var mode = GetPrivateField<VRMode>(configManager, "mode");

        // Determine the file name based on the VR mode
        int actualInt;
        float actualFloat;
        string actualString;

        if (mode == VRMode.Testbed)
        {
            actualInt = JsonUtility.FromJson<TestbedConfig>(json).test.testInt;
            actualFloat = JsonUtility.FromJson<TestbedConfig>(json).test.testFloat;
            actualString = JsonUtility.FromJson<TestbedConfig>(json).test.testString;
        }
        else
        {
            actualInt = JsonUtility.FromJson<UserVRConfig>(json).test.testInt;
            actualFloat = JsonUtility.FromJson<UserVRConfig>(json).test.testFloat;
            actualString = JsonUtility.FromJson<UserVRConfig>(json).test.testString;
        }

        return (actualInt, actualFloat, actualString);
    }


    public (int, float, string) ChangeTestProperties(IConfigManagerCommunicator _IConfigManagerCommunicator)
    {
        // Create a random integer, float, and string to modify the config
        int randomInt = Random.Range(0, 1000);
        float randomFloat = Random.Range(0f, 1000f);
        string randomString = "TestString_" + Random.Range(0, 1000);

        // Modify a property in the config
        _IConfigManagerCommunicator.ChangeProperty("testSettings.testInt", randomInt);
        _IConfigManagerCommunicator.ChangeProperty("testSettings.testFloat", randomFloat);
        _IConfigManagerCommunicator.ChangeProperty("testSettings.testString", randomString);

        return (randomInt, randomFloat, randomString);
    }


    private IEnumerator Wait(float time = 0.1f)
    {
        yield return new WaitForSeconds(time); // Wait until everything is assigned
    }
}


public class DummyConfigProvider : MonoBehaviour
{
    public string moduleName = "testSettings";

    public int testInt;
    public float testFloat;
    public string testString;

    public void bindThisModule(IConfigManagerConnector _IConfigManagerConnector)
    {
        // Bind this handler to the module
        //_IConfigManagerConnector.BindModule(this, moduleName);
    }
}

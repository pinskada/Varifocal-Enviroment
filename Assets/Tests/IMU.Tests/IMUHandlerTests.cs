using Contracts;
using UnityEngine;
using NUnit.Framework;
using System.Reflection;

public class IMUHandlerTests
{

    private ICameraHub _ICameraHub;
    private IConfigManagerConnector _IConfigManager;

    [UnityTest]
    public IEnume TestIMUHandlerInitialization()
    {
        // Create a GameObject to attach the IMUHandler component
        IMUHandler imuHandler = CreateIMUHandler();

        imuHandler.Start();

        object filter = GetPrivateField<object>(imuHandler, "filter");
        Assert.IsNull(filter, "Madgwick filter should be null after initialization in IMUHandler");

        object initialRotation = GetPrivateField<object>(imuHandler, "initialRotation");
        Assert.IsNull(initialRotation, "Initial rotation should not be null after initialization in IMUHandler");


        _ICameraHub = new DummyCameraHub();
        _IConfigManager = new DummyConfigManager();

        imuHandler.InjectModules(_ICameraHub, _IConfigManager);

    }


    public IMUHandler CreateIMUHandler()
    {
        // Create a dummy GameObject
        var go = new GameObject("TestObject");

        // Attach your MonoBehaviour (IMUHandler)
        var imuHandler = go.AddComponent<IMUHandler>();

        return imuHandler;
    }


    public static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field.GetValue(obj);
    }

}


public class DummyConfigManager : IConfigManagerConnector
{

    public void BindModule(object handler, string moduleName)
    {
        // Dummy implementation for testing
    }

    public VRMode GetVRType()
    {
        return VRMode.Testbed; // Dummy return value for testing
    }
}


public class DummyCameraHub : ICameraHub
{
    public Quaternion GetCurrentOrientation()
    {
        return Quaternion.identity; // Return a default orientation for testing
    }

    public void ApplyOrientation(Quaternion orientation)
    {
        // Dummy implementation for testing
    }
}

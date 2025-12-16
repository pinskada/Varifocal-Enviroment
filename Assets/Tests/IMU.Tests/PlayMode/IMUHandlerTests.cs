using Contracts;
using UnityEngine;
using NUnit.Framework;
using System.Reflection;
using System.Collections;
using UnityEngine.TestTools;

public class TestGlobals
{
    // Global test variables

    public readonly Quaternion RandomQuaternion = new Quaternion(
        Random.Range(-1f, 1f),
        Random.Range(-1f, 1f),
        Random.Range(-1f, 1f),
        Random.Range(-1f, 1f)
    ).normalized;

    public readonly Quaternion DefaultQuaternion = new Quaternion(
        0f,
        0f,
        0f,
        1f
    );

    public Quaternion AppliedOrientation;
    public int AppliedCount = 0;
    public float TimeStamp = -0.1f;
    public Quaternion GenerateRandomQuaternion()
    {
        return new Quaternion(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    public (Vector3 gyro, Vector3 acc, Vector3 mag, float timeStamp) GetNewIMUData()
    {
        Vector3 gyro = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        Vector3 acc = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        Vector3 mag = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        TimeStamp += 0.1f;

        return (gyro, acc, mag, TimeStamp);
    }
}

public class IMUHandlerTests
{

    private ICameraAligner _ICameraAligner;
    //private IConfigManagerCommunicator _IConfigManager;

    [UnityTest]
    public IEnumerator TestInit()
    {
        TestGlobals testGlobals = new TestGlobals();

        // Create a GameObject to attach the IMUHandler component
        IMUHandler imuHandler = CreateIMUHandler();

        object uninitilisedFilter = GetPrivateField<object>(imuHandler, "filter");
        Assert.IsNull(uninitilisedFilter, "Madgwick filter should be null after initialization in IMUHandler");

        PassDummyModules(imuHandler, testGlobals);

        yield return new WaitForSeconds(0.2f);

        object initialRotation = GetPrivateField<object>(imuHandler, "initialRotation");
        Assert.AreEqual(testGlobals.RandomQuaternion, initialRotation, "Initial rotation does not match the expected value");

        object initilisedFilter = GetPrivateField<object>(imuHandler, "filter");
        Assert.IsNotNull(initilisedFilter, "Madgwick filter should be initialized after starting in IMUHandler");
    }


    [UnityTest]
    public IEnumerator TestUpdateFilter()
    {
        TestGlobals testGlobals = new TestGlobals();

        // Create a GameObject to attach the IMUHandler component
        IMUHandler imuHandler = CreateIMUHandler();

        PassDummyModules(imuHandler, testGlobals);

        yield return new WaitForSeconds(0.01f);

        var filterBefore = GetPrivateField<Madgwick>(imuHandler, "filter");
        float[] q1 = filterBefore.Quaternion;
        Quaternion quaterBefore = new Quaternion(q1[0], q1[1], q1[2], q1[3]);


        // Test for timeStamp being infinity
        for (int i = 1; i <= 20; i++)
        {
            (Vector3 gyro, Vector3 acc, Vector3 mag, float timeStamp) = testGlobals.GetNewIMUData();

            timeStamp = float.PositiveInfinity;
            //IMUData imuData = new IMUData(gyro, acc, mag, timeStamp);
            //imuHandler.UpdateFilter(imuData);
        }

        var notUpdatedFilter2 = GetPrivateField<Madgwick>(imuHandler, "filter");

        float[] q2 = notUpdatedFilter2.Quaternion;
        Quaternion notUpdatedquater2 = new Quaternion(q2[0], q2[1], q2[2], q2[3]);

        Assert.AreEqual(quaterBefore, notUpdatedquater2, "UpdateFilter did not return when TimeStamp = infinity.");


        // Test for correct input arguments
        for (int i = 1; i <= 20; i++)
        {
            (Vector3 gyro, Vector3 acc, Vector3 mag, float timeStamp) = testGlobals.GetNewIMUData();

            //IMUData imuData = new IMUData(gyro, acc, mag, timeStamp);

            //imuHandler.UpdateFilter(imuData);
        }

        yield return null;

        var initilisedFilter = GetPrivateField<Madgwick>(imuHandler, "filter");

        float[] q4 = initilisedFilter.Quaternion;
        Quaternion quaterAfter = new Quaternion(q4[0], q4[1], q4[2], q4[3]);


        Assert.AreNotEqual(quaterBefore, quaterAfter, "Filter values remained unchanged.");
    }


    [UnityTest]
    public IEnumerator TestLateUpdate()
    {
        TestGlobals testGlobals = new TestGlobals();

        // Create a GameObject to attach the IMUHandler component
        IMUHandler imuHandler = CreateIMUHandler();

        Assert.AreEqual(0, testGlobals.AppliedCount, "Applied count should be zero before modules are passed");

        PassDummyModules(imuHandler, testGlobals);

        yield return null;

        Assert.AreEqual(1, testGlobals.AppliedCount, "Applied count should be one after modules are passed");

        yield return null;

        Assert.AreEqual(2, testGlobals.AppliedCount, "Applied count should be two before reset");
    }


    [UnityTest]
    public IEnumerator TestResetOrientation()
    {
        TestGlobals testGlobals = new TestGlobals();

        // Create a GameObject to attach the IMUHandler component
        IMUHandler imuHandler = CreateIMUHandler();

        PassDummyModules(imuHandler, testGlobals);

        yield return null;

        imuHandler.ResetOrientation();

        Assert.AreEqual(2, testGlobals.AppliedCount, "Applied count should be two after reset");

        var initilisedFilter = GetPrivateField<Madgwick>(imuHandler, "filter");

        float[] qArr = initilisedFilter.Quaternion;
        Quaternion actualQuat = new Quaternion(qArr[0], qArr[1], qArr[2], qArr[3]);

        Assert.AreEqual(testGlobals.DefaultQuaternion, actualQuat, "Filter quaternion was not reset.");
    }


    [UnityTest]
    public IEnumerator TestChangeFilterSettings()
    {
        TestGlobals testGlobals = new TestGlobals();

        // Create a GameObject to attach the IMUHandler component
        IMUHandler imuHandler = CreateIMUHandler();

        PassDummyModules(imuHandler, testGlobals);

        yield return null;

        var filter = GetPrivateField<Madgwick>(imuHandler, "filter");

        (float betaMoving, float betaStill, float betaThreshold, float minGyroMagnitude) = new MadgwickTests().GenerateRandomParam();

        //imuHandler.ChangeFilterSettings(betaMoving, betaStill, betaThreshold, minGyroMagnitude);

        yield return null;

        Assert.AreEqual(betaMoving, GetPrivateField<float>(filter, "betaMoving"), "Beta moving not set correctly in ChangeFilterSettings");
        Assert.AreEqual(betaStill, GetPrivateField<float>(filter, "betaStill"), "Beta still not set correctly in ChangeFilterSettings");
        Assert.AreEqual(betaThreshold, GetPrivateField<float>(filter, "betaThreshold"), "Beta threshold not set correctly in ChangeFilterSettings");
        Assert.AreEqual(minGyroMagnitude, GetPrivateField<float>(filter, "minGyroMagnitude"), "Min gyro magnitude not set correctly in ChangeFilterSettings");
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


    public void PassDummyModules(IMUHandler imuHandler, TestGlobals testGlobals)
    {
        // _ICameraAligner = new DummyCameraAligner(testGlobals);
        //_IConfigManager = null; //new DummyConfigManager();

        //imuHandler.InjectModules(_ICameraHub, _IConfigManager);
    }

}

/*
public class DummyConfigManager : IConfigManagerCommunicator
{

    public void BindModule(IModuleSettingsHandler handler, string moduleName)
    {
        // Cast to the actual type
        IMUHandler imuHandler = handler as IMUHandler;
        if (imuHandler == null)
        {
            Debug.LogError("Handler is not of type IMUHandler");
            return;
        }

        // Dummy implementation for testing
        imuHandler.betaMoving = 0.01f;
        imuHandler.betaStill = 0.8f;
        imuHandler.betaThreshold = 0.5f;
        imuHandler.minGyroMagnitude = 0.0001f;
        imuHandler.minDt = 0.01f;
        imuHandler.maxDt = 0.1f;
    }

    public VRMode GetVRType()
    {
        return VRMode.Testbed; // Dummy return value for testing
    }
}

// */
// public class DummyCameraAligner : ICameraAligner
// {
//     TestGlobals testGlobals;

//     public DummyCameraAligner(TestGlobals globals)
//     {
//         testGlobals = globals;
//     }

//     public Quaternion GetCurrentOrientation()
//     {
//         return testGlobals.RandomQuaternion;
//     }

//     public void ApplyOrientation(Quaternion orientation)
//     {
//         testGlobals.AppliedCount++;
//         testGlobals.AppliedOrientation = orientation;
//     }
// }


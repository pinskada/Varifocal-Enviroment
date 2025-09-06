using UnityEngine;
using NUnit.Framework;
using System.Reflection;


public class MadgwickTests
{
    public float betaMoving = 0.1f;
    public float betaStill = 0.01f;
    public float betaThreshold = 0.5f;
    public float minGyroMagnitude = 0.1f;

    public (float betaMoving, float betaStill, float betaThreshold, float minGyroMagnitude) GenerateRandomParam()
    {
        betaMoving = Random.Range(0.01f, 1f);
        betaStill = Random.Range(betaMoving, 1f);
        betaThreshold = (betaStill + betaMoving) / 2;
        minGyroMagnitude = Random.Range(0.01f, 0.5f);

        return (
            betaMoving,
            betaStill,
            betaThreshold,
            minGyroMagnitude
        );
    }


    [Test]
    public void TestConstructor()
    {
        Madgwick filter = CreateFilter();
        Assert.AreEqual(betaMoving, GetPrivateField<float>(filter, "betaMoving"), "Beta moving not set correctly in constructor");
        Assert.AreEqual(betaStill, GetPrivateField<float>(filter, "betaStill"), "Beta still not set correctly in constructor");
        Assert.AreEqual(betaThreshold, GetPrivateField<float>(filter, "betaThreshold"), "Beta threshold not set correctly in constructor");
        Assert.AreEqual(minGyroMagnitude, GetPrivateField<float>(filter, "minGyroMagnitude"), "Min gyro magnitude not set correctly in constructor");
    }


    [Test]
    public void TestSetters()
    {
        Madgwick filter = CreateFilter();
        (betaMoving, betaStill, betaThreshold, minGyroMagnitude) = GenerateRandomParam();

        Debug.Log("Setting new params: " +
            $"betaMoving={betaMoving}, " +
            $"betaStill={betaStill}, " +
            $"betaThreshold={betaThreshold}, " +
            $"minGyroMagnitude={minGyroMagnitude}"
        );

        float samplePeriod = Random.Range(0.001f, 0.1f);

        filter.SetSamplePeriod(samplePeriod);
        filter.SetBetas(betaMoving, betaStill);
        filter.SetBetaThreshold(betaThreshold);
        filter.SetMinGyroMagnitude(minGyroMagnitude);

        Assert.AreEqual(samplePeriod, filter.samplePeriod, "Sample period not set correctly in setter");
        Assert.AreEqual(betaMoving, GetPrivateField<float>(filter, "betaMoving"), "Beta moving not set correctly in setter");
        Assert.AreEqual(betaStill, GetPrivateField<float>(filter, "betaStill"), "Beta still not set correctly in setter");
        Assert.AreEqual(betaThreshold, GetPrivateField<float>(filter, "betaThreshold"), "Beta threshold not set correctly in setter");
        Assert.AreEqual(minGyroMagnitude, GetPrivateField<float>(filter, "minGyroMagnitude"), "Min gyro magnitude not set correctly in setter");
    }


    public Madgwick CreateFilter()
    {
        (betaMoving, betaStill, betaThreshold, minGyroMagnitude) = GenerateRandomParam();
        Madgwick filter = new Madgwick(betaMoving, betaStill, betaThreshold, minGyroMagnitude);
        return filter;
    }


    public static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field.GetValue(obj);
    }

}
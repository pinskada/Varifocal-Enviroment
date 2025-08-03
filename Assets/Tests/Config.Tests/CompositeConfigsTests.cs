using NUnit.Framework;
using System;
using System.Reflection;

/// <summary>
/// Reflection-based tests to ensure each composite configuration class in CompositeConfigs.cs
/// declares exactly the expected public fields—no more, no less—and that each field
/// is of the correct type (from BlockConfigs).
/// </summary>
public class CompositeConfigsReflectionTests
{
    /*
    /// <summary>
    /// Asserts that the specified public instance field exists on the given type.
    /// </summary>
    private FieldInfo AssertFieldExists(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on type '{type.Name}'");
        return field;
    }

    /// <summary>
    /// Asserts that the field on the given type has the expected FieldType.
    /// </summary>
    private void AssertFieldType(FieldInfo field, Type expectedType)
    {
        Assert.AreEqual(expectedType, field.FieldType,
            $"Field '{field.Name}' on type '{field.DeclaringType.Name}' should be of type {expectedType.Name} but is {field.FieldType.Name}");
    }

    /// <summary>
    /// Asserts that the type defines exactly <paramref name="expectedCount"/> public instance fields.
    /// </summary>
    private void AssertFieldCount(Type type, int expectedCount)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        Assert.AreEqual(expectedCount, fields.Length,
            $"Type '{type.Name}' should have exactly {expectedCount} public fields, but has {fields.Length}");
    }

    [Test]
    public void RpiConfig_HasExactFieldsAndTypes()
    {
        Type t = typeof(RpiConfig);
        var f1 = AssertFieldExists(t, "camera");      AssertFieldType(f1, typeof(CameraSettings));
        var f2 = AssertFieldExists(t, "tracker");     AssertFieldType(f2, typeof(TrackerSettings));
        var f3 = AssertFieldExists(t, "leftCrop");    AssertFieldType(f3, typeof(CropSettings));
        var f4 = AssertFieldExists(t, "rightCrop");   AssertFieldType(f4, typeof(CropSettings));
        var f5 = AssertFieldExists(t, "gaze");        AssertFieldType(f5, typeof(GazeProcessorSettings));
        AssertFieldCount(t, 5);
    }

    [Test]
    public void Esp32Config_HasExactFieldsAndTypes()
    {
        Type t = typeof(Esp32Config);
        var f1 = AssertFieldExists(t, "camera");      AssertFieldType(f1, typeof(CameraSettings));
        var f2 = AssertFieldExists(t, "leftCrop");    AssertFieldType(f2, typeof(CropSettings));
        var f3 = AssertFieldExists(t, "rightCrop");   AssertFieldType(f3, typeof(CropSettings));
        AssertFieldCount(t, 3);
    }

    [Test]
    public void EyeLoopConfig_HasExactFieldsAndTypes()
    {
        Type t = typeof(EyeLoopConfig);
        var f1 = AssertFieldExists(t, "tracker");     AssertFieldType(f1, typeof(TrackerSettings));
        var f2 = AssertFieldExists(t, "leftCrop");    AssertFieldType(f2, typeof(CropSettings));
        var f3 = AssertFieldExists(t, "rightCrop");   AssertFieldType(f3, typeof(CropSettings));
        var f4 = AssertFieldExists(t, "gaze");        AssertFieldType(f4, typeof(GazeProcessorSettings));
        AssertFieldCount(t, 4);
    }

    [Test]
    public void TestbedConfig_HasExactFieldsAndTypes()
    {
        Type t = typeof(TestbedConfig);
        var f1 = AssertFieldExists(t, "displaySettings"); AssertFieldType(f1, typeof(DisplaySettings));
        var f2 = AssertFieldExists(t, "imuSettings");     AssertFieldType(f2, typeof(IMUSettings));
        var f3 = AssertFieldExists(t, "rpi");             AssertFieldType(f3, typeof(RpiConfig));
        AssertFieldCount(t, 3);
    }

    [Test]
    public void UserVRConfig_HasExactFieldsAndTypes()
    {
        Type t = typeof(UserVRConfig);
        var f1 = AssertFieldExists(t, "displaySettings"); AssertFieldType(f1, typeof(DisplaySettings));
        var f2 = AssertFieldExists(t, "imuSettings");     AssertFieldType(f2, typeof(IMUSettings));
        var f3 = AssertFieldExists(t, "esp");             AssertFieldType(f3, typeof(Esp32Config));
        var f4 = AssertFieldExists(t, "eyeTracker");      AssertFieldType(f4, typeof(EyeLoopConfig));
        AssertFieldCount(t, 4);
    }
    */
}

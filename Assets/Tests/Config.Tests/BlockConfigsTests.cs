using NUnit.Framework;
using System;
using System.Reflection;

/// <summary>
/// Reflection-based tests to ensure each block-level configuration class in BlockConfigs.cs
/// declares exactly the expected public fields—no more, no less—and that each field
/// is of the correct type.
/// </summary>
public class BlockConfigsReflectionTests
{
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
    public void DisplaySettings_HasExactFieldsAndTypes()
    {
        Type t = typeof(DisplaySettings);
        var f1 = AssertFieldExists(t, "camIPD"); AssertFieldType(f1, typeof(float));
        var f2 = AssertFieldExists(t, "dispWidth"); AssertFieldType(f2, typeof(float));
        var f3 = AssertFieldExists(t, "dispHeight"); AssertFieldType(f3, typeof(float));
        var f4 = AssertFieldExists(t, "eyeToScreen"); AssertFieldType(f4, typeof(float));
        AssertFieldCount(t, 4);
    }

    [Test]
    public void IMUSettings_HasExactFieldsAndTypes()
    {
        Type t = typeof(IMUSettings);
        var f1 = AssertFieldExists(t, "Alfa"); AssertFieldType(f1, typeof(float));
        var f2 = AssertFieldExists(t, "Beta"); AssertFieldType(f2, typeof(float));
        var f3 = AssertFieldExists(t, "Fs"); AssertFieldType(f3, typeof(float));
        var f4 = AssertFieldExists(t, "type"); AssertFieldType(f4, typeof(float));
        AssertFieldCount(t, 4);
    }

    [Test]
    public void CameraSettings_HasExactFieldsAndTypes()
    {
        Type t = typeof(CameraSettings);
        var f1 = AssertFieldExists(t, "resWidth"); AssertFieldType(f1, typeof(int));
        var f2 = AssertFieldExists(t, "resHeight"); AssertFieldType(f2, typeof(int));
        var f3 = AssertFieldExists(t, "focus"); AssertFieldType(f3, typeof(int));
        var f4 = AssertFieldExists(t, "exposure"); AssertFieldType(f4, typeof(int));
        var f5 = AssertFieldExists(t, "gain"); AssertFieldType(f5, typeof(int));
        var f6 = AssertFieldExists(t, "jpegQuality"); AssertFieldType(f6, typeof(int));
        var f7 = AssertFieldExists(t, "previewFps"); AssertFieldType(f7, typeof(int));
        AssertFieldCount(t, 7);
    }

    [Test]
    public void CropSettings_HasExactFieldsAndTypes()
    {
        Type t = typeof(CropSettings);
        var f1 = AssertFieldExists(t, "left"); AssertFieldType(f1, typeof(float));
        var f2 = AssertFieldExists(t, "right"); AssertFieldType(f2, typeof(float));
        var f3 = AssertFieldExists(t, "top"); AssertFieldType(f3, typeof(float));
        var f4 = AssertFieldExists(t, "bottom"); AssertFieldType(f4, typeof(float));
        AssertFieldCount(t, 4);
    }

    [Test]
    public void TrackerSettings_HasExactFieldsAndTypes()
    {
        Type t = typeof(TrackerSettings);
        var f1 = AssertFieldExists(t, "minRadius"); AssertFieldType(f1, typeof(int));
        var f2 = AssertFieldExists(t, "maxRadius"); AssertFieldType(f2, typeof(int));
        var f3 = AssertFieldExists(t, "searchStep"); AssertFieldType(f3, typeof(int));
        AssertFieldCount(t, 3);
    }

    [Test]
    public void GazeProcessorSettings_HasExactFieldsAndTypes()
    {
        Type t = typeof(GazeProcessorSettings);
        var f1 = AssertFieldExists(t, "alphaVal"); AssertFieldType(f1, typeof(float));
        var f2 = AssertFieldExists(t, "bufferCropFactor"); AssertFieldType(f2, typeof(float));
        var f3 = AssertFieldExists(t, "dataStdThreshold"); AssertFieldType(f3, typeof(float));
        var f4 = AssertFieldExists(t, "gyroThreshold"); AssertFieldType(f4, typeof(float));
        AssertFieldCount(t, 4);
    }
}

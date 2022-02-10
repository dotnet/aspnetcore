// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests;

public class DynamicObjectIntegrationTest
{
    [Fact]
    public void AddResults_ShouldReplaceExistingPropertyValue_InNestedDynamicObject()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.Nested = new NestedObject();
        dynamicTestObject.Nested.DynamicProperty = new DynamicTestObject();
        dynamicTestObject.Nested.DynamicProperty.InBetweenFirst = new DynamicTestObject();
        dynamicTestObject.Nested.DynamicProperty.InBetweenFirst.InBetweenSecond = new DynamicTestObject();
        dynamicTestObject.Nested.DynamicProperty.InBetweenFirst.InBetweenSecond.StringProperty = "A";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Add("/Nested/DynamicProperty/InBetweenFirst/InBetweenSecond/StringProperty", "B");

        // Act
        patchDocument.ApplyTo(dynamicTestObject);

        // Assert
        Assert.Equal("B", dynamicTestObject.Nested.DynamicProperty.InBetweenFirst.InBetweenSecond.StringProperty);
    }

    [Fact]
    public void ShouldNotBeAbleToAdd_ToNonExistingProperty_ThatIsNotTheRoot()
    {
        //Adding to a Nonexistent Target
        //
        //   An example target JSON document:
        //   { "foo": "bar" }
        //   A JSON Patch document:
        //   [
        //        { "op": "add", "path": "/baz/bat", "value": "qux" }
        //      ]
        //   This JSON Patch document, applied to the target JSON document above,
        //   would result in an error (therefore, it would not be applied),
        //   because the "add" operation's target location that references neither
        //   the root of the document, nor a member of an existing object, nor a
        //   member of an existing array.

        // Arrange
        var nestedObject = new NestedObject()
        {
            DynamicProperty = new DynamicTestObject()
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Add("DynamicProperty/OtherProperty/IntProperty", 1);

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.ApplyTo(nestedObject);
        });

        // Assert
        Assert.Equal("The target location specified by path segment 'OtherProperty' was not found.", exception.Message);
    }

    [Fact]
    public void CopyProperties_InNestedDynamicObject()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.NestedDynamicObject = new DynamicTestObject();
        dynamicTestObject.NestedDynamicObject.StringProperty = "A";
        dynamicTestObject.NestedDynamicObject.AnotherStringProperty = "B";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("NestedDynamicObject/StringProperty", "NestedDynamicObject/AnotherStringProperty");

        // Act
        patchDocument.ApplyTo(dynamicTestObject);

        // Assert
        Assert.Equal("A", dynamicTestObject.NestedDynamicObject.AnotherStringProperty);
    }

    [Fact]
    public void MoveToNonExistingProperty_InDynamicObject_ShouldAddNewProperty()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.StringProperty = "A";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Move("StringProperty", "AnotherStringProperty");

        // Act
        patchDocument.ApplyTo(dynamicTestObject);
        dynamicTestObject.TryGetValue("StringProperty", out object valueFromDictionary);

        // Assert
        Assert.Equal("A", dynamicTestObject.AnotherStringProperty);
        Assert.Null(valueFromDictionary);
    }

    [Fact]
    public void MovePropertyValue_FromDynamicObject_ToTypedObject()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.StringProperty = "A";
        dynamicTestObject.SimpleObject = new SimpleObject() { AnotherStringProperty = "B" };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Move("StringProperty", "SimpleObject/AnotherStringProperty");

        // Act
        patchDocument.ApplyTo(dynamicTestObject);
        dynamicTestObject.TryGetValue("StringProperty", out object valueFromDictionary);

        // Assert
        Assert.Equal("A", dynamicTestObject.SimpleObject.AnotherStringProperty);
        Assert.Null(valueFromDictionary);
    }

    [Fact]
    public void RemoveNestedProperty_FromDynamicObject()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.Test = new DynamicTestObject();
        dynamicTestObject.Test.AnotherTest = "A";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("Test");

        // Act
        patchDocument.ApplyTo(dynamicTestObject);
        dynamicTestObject.TryGetValue("Test", out object valueFromDictionary);

        // Assert
        Assert.Null(valueFromDictionary);
    }

    [Fact]
    public void RemoveFromNestedObject_InDynamicObject_MixedCase_ThrowsPathNotFoundException()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.SimpleObject = new SimpleObject()
        {
            StringProperty = "A"
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("Simpleobject/stringProperty");

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.ApplyTo(dynamicTestObject);
        });

        // Assert
        Assert.Equal("The target location specified by path segment 'Simpleobject' was not found.", exception.Message);
    }

    [Fact]
    public void ReplaceNestedTypedObject_InDynamicObject()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.SimpleObject = new SimpleObject()
        {
            IntegerValue = 5,
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var newObject = new SimpleObject()
        {
            DoubleValue = 1
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace("SimpleObject", newObject);

        // Act
        patchDocument.ApplyTo(dynamicTestObject);

        // Assert
        Assert.Equal(1, dynamicTestObject.SimpleObject.DoubleValue);
        Assert.Equal(0, dynamicTestObject.SimpleObject.IntegerValue);
        Assert.Null(dynamicTestObject.SimpleObject.IntegerList);
    }

    [Fact]
    public void TestStringPropertyValue_IsSuccessful()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.Property = "A";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Test("Property", "A");

        // Act & Assert
        patchDocument.ApplyTo(dynamicTestObject);
    }

    [Fact]
    public void TestIntegerPropertyValue_ThrowsJsonPatchException_IfTestFails()
    {
        // Arrange
        dynamic dynamicTestObject = new DynamicTestObject();
        dynamicTestObject.Nested = new SimpleObject()
        {
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Test("Nested/IntegerList/0", 2);

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.ApplyTo(dynamicTestObject);
        });

        // Assert
        Assert.Equal("The current value '1' at position '0' is not equal to the test value '2'.", exception.Message);
    }
}

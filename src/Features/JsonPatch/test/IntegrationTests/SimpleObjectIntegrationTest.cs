// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Dynamic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests;

public class SimpleObjectIntegrationTest
{
    [Fact]
    public void TestDoubleValueProperty()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            DoubleValue = 9.8
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Test("DoubleValue", 9.8);

        // Act & Assert
        patchDocument.ApplyTo(targetObject);
    }

    [Fact]
    public void CopyStringProperty_ToAnotherStringProperty()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            StringProperty = "A",
            AnotherStringProperty = "B"
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("StringProperty", "AnotherStringProperty");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal("A", targetObject.AnotherStringProperty);
    }

    [Fact]
    public void CopyNullStringProperty_ToAnotherStringProperty()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            StringProperty = null,
            AnotherStringProperty = "B"
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("StringProperty", "AnotherStringProperty");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Null(targetObject.AnotherStringProperty);
    }

    [Fact]
    public void MoveIntegerProperty_ToAnotherIntegerProperty()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerValue = 2,
            AnotherIntegerValue = 3
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Move("IntegerValue", "AnotherIntegerValue");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(2, targetObject.AnotherIntegerValue);
        Assert.Equal(0, targetObject.IntegerValue);
    }

    [Fact]
    public void RemoveDecimalPropertyValue()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            DecimalValue = 9.8M
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("DecimalValue");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(0, targetObject.DecimalValue);
    }

    [Fact]
    public void ReplaceGuid()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            GuidValue = Guid.NewGuid()
        };

        var newGuid = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace("GuidValue", newGuid);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(newGuid, targetObject.GuidValue);
    }

    [Fact]
    public void AddReplacesGuid()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            GuidValue = Guid.NewGuid()
        };

        var newGuid = Guid.NewGuid();
        var patchDocument = new JsonPatchDocument();
        patchDocument.Add("GuidValue", newGuid);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(newGuid, targetObject.GuidValue);
    }

    // https://github.com/dotnet/aspnetcore/issues/3634
    [Fact]
    public void Regression_AspNetCore3634()
    {
        // Assert
        var document = new JsonPatchDocument();
        document.Move("/Object", "/Object/goodbye");

        dynamic @object = new ExpandoObject();
        @object.hello = "world";

        var target = new Regression_AspNetCore3634_Object();
        target.Object = @object;

        // Act
        var ex = Assert.Throws<JsonPatchException>(() => document.ApplyTo(target));

        // Assert
        Assert.Equal("For operation 'move', the target location specified by path '/Object/goodbye' was not found.", ex.Message);
    }

    private class Regression_AspNetCore3634_Object
    {
        public dynamic Object { get; set; }
    }
}

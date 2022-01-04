// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

public class SerializableErrorTests
{
    [Fact]
    public void ConvertsModelState_To_Dictionary()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("key1", "Test Error 1");
        modelState.AddModelError("key1", "Test Error 2");
        modelState.AddModelError("key2", "Test Error 3");

        // Act
        var serializableError = new SerializableError(modelState);

        // Assert
        var arr = Assert.IsType<string[]>(serializableError["key1"]);
        Assert.Equal("Test Error 1", arr[0]);
        Assert.Equal("Test Error 2", arr[1]);
        Assert.Equal("Test Error 3", (serializableError["key2"] as string[])[0]);
    }

    [Fact]
    public void LookupIsCaseInsensitive()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("key1", "x");

        // Act
        var serializableError = new SerializableError(modelState);

        // Assert
        var arr = Assert.IsType<string[]>(serializableError["KEY1"]);
        Assert.Equal("x", arr[0]);
    }

    [Fact]
    public void ConvertsModelState_To_Dictionary_AddsDefaultValuesWhenErrorsAreAbsent()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("key1", "");

        // Act
        var serializableError = new SerializableError(modelState);

        // Assert
        var arr = Assert.IsType<string[]>(serializableError["key1"]);
        Assert.Equal("The input was not valid.", arr[0]);
    }

    [Fact]
    public void DoesNotThrowOnValidModelState()
    {
        // Arrange, Act & Assert (does not throw)
        new SerializableError(new ModelStateDictionary());
    }

    [Fact]
    public void DoesNotAddEntries_IfNoErrorsArePresent()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.SetModelValue("key1", "value1", "value1");
        modelState.SetModelValue("key2", "value2", "value2");

        // Act
        var serializableError = new SerializableError(modelState);

        // Assert
        Assert.Empty(serializableError);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

public class JSObjectReferenceJsonConverterTest
{
    private readonly JsonSerializerOptions JsonSerializerOptions;

    public JSObjectReferenceJsonConverterTest()
    {
        JsonSerializerOptions = DefaultWebAssemblyJSRuntime.Instance.JsonSerializerOptions;
    }

    [Fact]
    public void Read_ReadsJson_IJSObjectReference()
    {
        // Arrange
        var expectedId = 3;
        var json = $"{{\"__jsObjectId\":{expectedId}}}";

        // Act
        var deserialized = (JSObjectReference)JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(expectedId, deserialized?.Id);
    }

    [Fact]
    public void Read_ReadsJson_IJSInProcessObjectReference()
    {
        // Arrange
        var expectedId = 3;
        var json = $"{{\"__jsObjectId\":{expectedId}}}";

        // Act
        var deserialized = (JSInProcessObjectReference)JsonSerializer.Deserialize<IJSInProcessObjectReference>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(expectedId, deserialized?.Id);
    }
}

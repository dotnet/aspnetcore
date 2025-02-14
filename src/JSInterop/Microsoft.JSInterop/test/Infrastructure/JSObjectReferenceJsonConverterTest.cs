// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Infrastructure;

public class JSObjectReferenceJsonConverterTest
{
    private readonly JSRuntime JSRuntime = new TestJSRuntime();
    private readonly JsonSerializerOptions JsonSerializerOptions;

    public JSObjectReferenceJsonConverterTest()
    {
        JsonSerializerOptions = JSRuntime.JsonSerializerOptions;
        JsonSerializerOptions.Converters.Add(new JSObjectReferenceJsonConverter(JSRuntime));
    }

    [Fact]
    public void Read_Throws_IfJsonIsMissingJSObjectIdProperty()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
        Assert.Equal("Required property __jsObjectId not found.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonContainsUnknownContent()
    {
        // Arrange
        var json = "{\"foo\":2}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
        Assert.Equal("Unexpected JSON property foo.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonIsIncomplete()
    {
        // Arrange
        var json = $"{{\"__jsObjectId\":5";

        // Act & Assert
        var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
        Assert.IsAssignableFrom<JsonException>(ex);
    }

    [Fact]
    public void Read_Throws_IfJSObjectIdAppearsMultipleTimes()
    {
        // Arrange
        var json = $"{{\"__jsObjectId\":3,\"__jsObjectId\":7}}";

        // Act & Assert
        var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
        Assert.IsAssignableFrom<JsonException>(ex);
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
    public void Write_WritesValidJson()
    {
        // Arrange
        var jsObjectRef = new JSObjectReference(JSRuntime, 7);

        // Act
        var json = JsonSerializer.Serialize((IJSObjectReference)jsObjectRef, JsonSerializerOptions);

        // Assert
        Assert.Equal($"{{\"__jsObjectId\":{jsObjectRef.Id}}}", json);
    }
}

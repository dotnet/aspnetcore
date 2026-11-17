// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Infrastructure;

public class JSStreamReferenceJsonConverterTest
{
    private readonly JSRuntime JSRuntime = new TestJSRuntime();
    private readonly JsonSerializerOptions JsonSerializerOptions;

    public JSStreamReferenceJsonConverterTest()
    {
        JsonSerializerOptions = JSRuntime.JsonSerializerOptions;
        JsonSerializerOptions.Converters.Add(new JSStreamReferenceJsonConverter(JSRuntime));
    }

    [Fact]
    public void Read_Throws_IfJsonIsMissingJSObjectIdProperty()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions));
        Assert.Equal("Required property __jsObjectId not found.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonContainsUnknownContent()
    {
        // Arrange
        var json = "{\"foo\":2}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions));
        Assert.Equal("Unexpected JSON property foo.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonIsIncomplete()
    {
        // Arrange
        var json = $"{{\"__jsObjectId\":5";

        // Act & Assert
        var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions));
        Assert.IsAssignableFrom<JsonException>(ex);
    }

    [Fact]
    public void Read_Throws_IfJSObjectIdAppearsMultipleTimes()
    {
        // Arrange
        var json = $"{{\"__jsObjectId\":3,\"__jsObjectId\":7}}";

        // Act & Assert
        var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions));
        Assert.IsAssignableFrom<JsonException>(ex);
    }

    [Fact]
    public void Read_Throws_IfLengthNotProvided()
    {
        // Arrange
        var expectedId = 3;
        var json = $"{{\"__jsObjectId\":{expectedId}}}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions));
        Assert.Equal("Required property __jsStreamReferenceLength not found.", ex.Message);
    }

    [Fact]
    public void Read_ReadsJson_IJSStreamReference()
    {
        // Arrange
        var expectedId = 3;
        var expectedLength = 5;
        var json = $"{{\"__jsObjectId\":{expectedId}, \"__jsStreamReferenceLength\":{expectedLength}}}";

        // Act
        var deserialized = (JSStreamReference)JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(expectedId, deserialized?.Id);
        Assert.Equal(expectedLength, deserialized?.Length);
    }

    [Fact]
    public void Read_ReadsJson_IJSStreamReferenceReverseOrder()
    {
        // Arrange
        var expectedId = 3;
        var expectedLength = 5;
        var json = $"{{\"__jsStreamReferenceLength\":{expectedLength}, \"__jsObjectId\":{expectedId}}}";

        // Act
        var deserialized = (JSStreamReference)JsonSerializer.Deserialize<IJSStreamReference>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(expectedId, deserialized?.Id);
        Assert.Equal(expectedLength, deserialized?.Length);
    }

    [Fact]
    public void Write_WritesValidJson()
    {
        // Arrange
        var jsObjectRef = new JSStreamReference(JSRuntime, 7, 10);

        // Act
        var json = JsonSerializer.Serialize((IJSStreamReference)jsObjectRef, JsonSerializerOptions);

        // Assert
        Assert.Equal($"{{\"__jsObjectId\":{jsObjectRef.Id}}}", json);
    }
}

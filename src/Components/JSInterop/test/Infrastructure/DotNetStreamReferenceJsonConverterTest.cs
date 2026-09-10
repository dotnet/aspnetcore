// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

public class DotNetStreamReferenceJsonConverterTest
{
    private readonly JSRuntime JSRuntime = new TestJSRuntime();
    private readonly JsonSerializerOptions JsonSerializerOptions;

    public DotNetStreamReferenceJsonConverterTest()
    {
        JsonSerializerOptions = JSRuntime.JsonSerializerOptions;
        JsonSerializerOptions.Converters.Add(new DotNetStreamReferenceJsonConverter(JSRuntime));
    }

    [Fact]
    public void Read_Throws()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<DotNetStreamReference>(json, JsonSerializerOptions));
        Assert.StartsWith("DotNetStreamReference cannot be supplied from JavaScript to .NET because the stream contents have already been transferred.", ex.Message);
    }

    [Fact]
    public void Write_WritesValidJson()
    {
        // Arrange
        var streamRef = new DotNetStreamReference(new MemoryStream());

        // Act
        var json = JsonSerializer.Serialize(streamRef, JsonSerializerOptions);

        // Assert
        Assert.Equal("{\"__dotNetStream\":1}", json);
    }

    [Fact]
    public void Write_WritesMultipleValidJson()
    {
        // Arrange
        var streamRef = new DotNetStreamReference(new MemoryStream());

        // Act & Assert
        for (var i = 1; i <= 10; i++)
        {
            var json = JsonSerializer.Serialize(streamRef, JsonSerializerOptions);
            Assert.Equal($"{{\"__dotNetStream\":{i}}}", json);
        }
    }
}

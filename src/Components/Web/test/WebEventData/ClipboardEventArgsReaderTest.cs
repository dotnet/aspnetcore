// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class ClipboardEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new ClipboardEventArgs
        {
            Type = "Some type"
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = ClipboardEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.Type, result.Type);
    }

    private static JsonElement GetJsonElement(ClipboardEventArgs args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

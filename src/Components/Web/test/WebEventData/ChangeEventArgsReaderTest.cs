// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class ChangeEventArgsReaderTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Read_WithBoolValue(bool changeValue)
    {
        // Arrange
        var args = new ChangeEventArgs
        {
            Value = changeValue,
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = ChangeEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.Value, result.Value);
    }

    [Fact]
    public void Read_WithNullValue()
    {
        // Arrange
        var args = new ChangeEventArgs
        {
            Value = null,
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = ChangeEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.Value, result.Value);
    }

    [Fact]
    public void Read_WithStringValue()
    {
        // Arrange
        var args = new ChangeEventArgs
        {
            Value = "Hello world",
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = ChangeEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.Value, result.Value);
    }

    private static JsonElement GetJsonElement(ChangeEventArgs args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

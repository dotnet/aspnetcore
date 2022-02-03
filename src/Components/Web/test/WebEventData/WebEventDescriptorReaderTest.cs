// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

public class WebEventDescriptorReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new WebEventDescriptor
        {
            EventFieldInfo = new EventFieldInfo
            {
                ComponentId = 89,
                FieldValue = "field1",
            },
            EventHandlerId = 897,
            EventName = "test1",
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = WebEventDescriptorReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.EventHandlerId, result.EventHandlerId);
        Assert.Equal(args.EventName, result.EventName);
        Assert.Equal(args.EventFieldInfo.ComponentId, result.EventFieldInfo.ComponentId);
        Assert.Equal(args.EventFieldInfo.FieldValue, result.EventFieldInfo.FieldValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Read_WithBoolValue_Works(bool value)
    {
        // Arrange
        var args = new WebEventDescriptor
        {
            EventFieldInfo = new EventFieldInfo
            {
                ComponentId = 89,
                FieldValue = value,
            },
            EventHandlerId = 897,
            EventName = "test1",
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = WebEventDescriptorReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.EventHandlerId, result.EventHandlerId);
        Assert.Equal(args.EventName, result.EventName);
        Assert.Equal(args.EventFieldInfo.ComponentId, result.EventFieldInfo.ComponentId);
        Assert.Equal(args.EventFieldInfo.FieldValue, result.EventFieldInfo.FieldValue);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

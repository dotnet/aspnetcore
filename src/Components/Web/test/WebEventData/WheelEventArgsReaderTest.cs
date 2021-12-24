// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class WheelEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new WheelEventArgs
        {
            AltKey = false,
            CtrlKey = true,
            MetaKey = true,
            ShiftKey = false,
            Type = "type1",
            Detail = 789,
            Button = 7,
            Buttons = 234,
            ClientX = 3.2,
            ClientY = 33.1,
            DeltaMode = 2,
            DeltaX = 11.1,
            DeltaY = 21.2,
            DeltaZ = 9.1,
            OffsetX = 7.2,
            OffsetY = 1.2,
            PageX = 5.6,
            PageY = 7.8,
            ScreenX = 3.56,
            ScreenY = 8.32,
        };

        var jsonElement = GetJsonElement(args);

        // Act
        var result = WheelEventArgsReader.Read(jsonElement);

        // Assert
        MouseEventArgsReaderTest.AssertEqual(args, result);
        Assert.Equal(args.DeltaMode, result.DeltaMode);
        Assert.Equal(args.DeltaX, result.DeltaX);
        Assert.Equal(args.DeltaY, result.DeltaY);
        Assert.Equal(args.DeltaZ, result.DeltaZ);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

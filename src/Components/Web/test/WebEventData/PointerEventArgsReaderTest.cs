// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class PointerEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new PointerEventArgs
        {
            AltKey = false,
            Button = 72,
            Buttons = 61,
            ClientX = 3.1,
            ClientY = 4.2,
            CtrlKey = true,
            PointerId = 17,
            PointerType = "pointer1",
            Width = 90.1f,
            Height = 43.87f,
            IsPrimary = true,
            Pressure = 0.8f,
            TiltX = 1.1f,
            TiltY = 9.2f,
            Type = "type2",
            Detail = 7,
            MetaKey = true,
            OffsetX = 8.2,
            OffsetY = 7.1,
            PageX = 5.6,
            PageY = 7.8,
            ScreenX = 0.1,
            ScreenY = 4.4,
            ShiftKey = true,
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = PointerEventArgsReader.Read(jsonElement);

        // Assert
        MouseEventArgsReaderTest.AssertEqual(args, result);
        Assert.Equal(args.PointerId, result.PointerId);
        Assert.Equal(args.PointerType, result.PointerType);
        Assert.Equal(args.Width, result.Width);
        Assert.Equal(args.Height, result.Height);
        Assert.Equal(args.IsPrimary, result.IsPrimary);
        Assert.Equal(args.Pressure, result.Pressure);
        Assert.Equal(args.TiltX, result.TiltX);
        Assert.Equal(args.TiltY, result.TiltY);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class TouchEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new TouchEventArgs
        {
            AltKey = false,
            CtrlKey = true,
            MetaKey = true,
            ShiftKey = false,
            Type = "type1",
            ChangedTouches = new[]
            {
                    new TouchPoint
                    {
                        ClientX = 1.3,
                        ClientY = 2.9,
                        Identifier = 11,
                        PageX = 7.0,
                        PageY = 0.23,
                        ScreenX = 1.0,
                        ScreenY = 39.2
                    },
                },
            Detail = 789,
            TargetTouches = new[]
            {
                    new TouchPoint
                    {
                        ClientX = 2.3,
                        ClientY = 22.9,
                        Identifier = 8,
                        PageX = 786.0,
                        PageY = 1.4,
                        ScreenX = 5.0,
                        ScreenY = 9.2
                    },
                    new TouchPoint
                    {
                        ClientX = 12.3,
                        ClientY = 2.9,
                        Identifier = 17,
                        PageX = 6.3,
                        PageY = 13.4,
                        ScreenX = 4.0,
                        ScreenY = 7.2
                    },
                },
            Touches = new[]
            {
                    new TouchPoint
                    {
                        ClientX = 6.8,
                        ClientY = 1.9,
                        Identifier = 3,
                        PageX = 2.3,
                        PageY = 3.4,
                        ScreenX = 4.1,
                        ScreenY = 8.0,
                    },
                },
        };

        var jsonElement = GetJsonElement(args);

        // Act
        var result = TouchEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.AltKey, result.AltKey);
        Assert.Equal(args.CtrlKey, result.CtrlKey);
        Assert.Equal(args.MetaKey, result.MetaKey);
        Assert.Equal(args.ShiftKey, result.ShiftKey);
        Assert.Equal(args.Type, result.Type);
        Assert.Equal(args.Detail, result.Detail);

        AssertEqual(args.Touches, result.Touches);
        AssertEqual(args.ChangedTouches, result.ChangedTouches);
        AssertEqual(args.TargetTouches, result.TargetTouches);
    }

    private void AssertEqual(TouchPoint[] expected, TouchPoint[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            AssertEqual(expected[i], actual[i]);
        }
    }

    private void AssertEqual(TouchPoint expected, TouchPoint actual)
    {
        Assert.Equal(expected.ClientX, actual.ClientX);
        Assert.Equal(expected.ClientY, actual.ClientY);
        Assert.Equal(expected.Identifier, actual.Identifier);
        Assert.Equal(expected.PageX, actual.PageX);
        Assert.Equal(expected.PageY, actual.PageY);
        Assert.Equal(expected.ScreenX, actual.ScreenX);
        Assert.Equal(expected.ScreenY, actual.ScreenY);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class KeyboardEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new KeyboardEventArgs
        {
            AltKey = true,
            Code = "code1",
            CtrlKey = false,
            Key = "key2",
            Location = 5.3f,
            MetaKey = false,
            Repeat = true,
            ShiftKey = true,
            Type = "type1",
            IsComposing = true,
        };

        var jsonElement = GetJsonElement(args);

        // Act
        var result = KeyboardEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.AltKey, result.AltKey);
        Assert.Equal(args.Code, result.Code);
        Assert.Equal(args.CtrlKey, result.CtrlKey);
        Assert.Equal(args.Key, result.Key);
        Assert.Equal(args.Location, result.Location);
        Assert.Equal(args.MetaKey, result.MetaKey);
        Assert.Equal(args.Repeat, result.Repeat);
        Assert.Equal(args.ShiftKey, result.ShiftKey);
        Assert.Equal(args.Type, result.Type);
        Assert.Equal(args.IsComposing, result.IsComposing);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class DragEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new DragEventArgs
        {
            AltKey = true,
            Button = 72,
            Buttons = 61,
            ClientX = 3.1,
            ClientY = 4.2,
            CtrlKey = false,
            DataTransfer = new()
            {
                DropEffect = "effect1",
                EffectAllowed = "allowed1",
                Files = new[] { "File1", "File2" },
                Items = new[]
                {
                        new DataTransferItem
                        {
                            Kind = "kind1",
                            Type = "type1,"
                        },
                        new DataTransferItem
                        {
                            Kind = "kind7",
                            Type = "type6,"
                        },
                    },
                Types = new[] { "type1", "type2", "type3" },
            },
            Detail = 7,
            MetaKey = false,
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
        var result = DragEventArgsReader.Read(jsonElement);

        // Assert
        MouseEventArgsReaderTest.AssertEqual(args, result);
        AssertEqual(args.DataTransfer, result.DataTransfer);
    }

    private void AssertEqual(DataTransfer expected, DataTransfer actual)
    {
        Assert.Equal(expected.DropEffect, actual.DropEffect);
        Assert.Equal(expected.EffectAllowed, actual.EffectAllowed);
        Assert.Equal(expected.Files, actual.Files);
        AssertEqual(expected.Items, actual.Items);
        Assert.Equal(expected.Types, actual.Types);
    }

    private void AssertEqual(DataTransferItem[] expected, DataTransferItem[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Kind, actual[i].Kind);
            Assert.Equal(expected[i].Type, actual[i].Type);
        }
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}

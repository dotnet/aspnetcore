// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class DragEventArgsReader
{
    private static readonly JsonEncodedText s_DataTransfer = JsonEncodedText.Encode("dataTransfer");
    private static readonly JsonEncodedText s_DropEffect = JsonEncodedText.Encode("dropEffect");
    private static readonly JsonEncodedText s_EffectAllowed = JsonEncodedText.Encode("effectAllowed");
    private static readonly JsonEncodedText s_Files = JsonEncodedText.Encode("files");
    private static readonly JsonEncodedText s_Items = JsonEncodedText.Encode("items");
    private static readonly JsonEncodedText s_Types = JsonEncodedText.Encode("types");
    private static readonly JsonEncodedText s_Kind = JsonEncodedText.Encode("kind");
    private static readonly JsonEncodedText s_Type = JsonEncodedText.Encode("type");

    internal static DragEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new DragEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_DataTransfer.EncodedUtf8Bytes))
            {
                eventArgs.DataTransfer = ReadDataTransfer(property.Value);
            }
            else
            {
                MouseEventArgsReader.ReadProperty(eventArgs, property);
            }
        }

        return eventArgs;
    }

    private static DataTransfer ReadDataTransfer(JsonElement jsonElement)
    {
        var dataTransfer = new DataTransfer();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_DropEffect.EncodedUtf8Bytes))
            {
                dataTransfer.DropEffect = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_EffectAllowed.EncodedUtf8Bytes))
            {
                dataTransfer.EffectAllowed = property.Value.GetString();
            }
            else if (property.NameEquals(s_Files.EncodedUtf8Bytes))
            {
                dataTransfer.Files = ReadStringArray(property.Value);
            }
            else if (property.NameEquals(s_Items.EncodedUtf8Bytes))
            {
                var value = property.Value;
                var items = new DataTransferItem[value.GetArrayLength()];
                var i = 0;
                foreach (var item in value.EnumerateArray())
                {
                    items[i++] = ReadDataTransferItem(item);
                }
                dataTransfer.Items = items;
            }
            else if (property.NameEquals(s_Types.EncodedUtf8Bytes))
            {
                dataTransfer.Types = ReadStringArray(property.Value);
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return dataTransfer;
    }

    private static DataTransferItem ReadDataTransferItem(JsonElement jsonElement)
    {
        var item = new DataTransferItem();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_Kind.EncodedUtf8Bytes))
            {
                item.Kind = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_Type.EncodedUtf8Bytes))
            {
                item.Type = property.Value.GetString()!;
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return item;
    }

    private static string[] ReadStringArray(JsonElement value)
    {
        var values = new string[value.GetArrayLength()];
        var i = 0;
        foreach (var item in value.EnumerateArray())
        {
            values[i++] = item.GetString()!;
        }

        return values;
    }
}

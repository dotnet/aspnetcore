// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class DragEventArgsReader
{
    private static readonly JsonEncodedText s_dataTransfer = JsonEncodedText.Encode("dataTransfer");
    private static readonly JsonEncodedText s_dropEffect = JsonEncodedText.Encode("dropEffect");
    private static readonly JsonEncodedText s_effectAllowed = JsonEncodedText.Encode("effectAllowed");
    private static readonly JsonEncodedText s_files = JsonEncodedText.Encode("files");
    private static readonly JsonEncodedText s_items = JsonEncodedText.Encode("items");
    private static readonly JsonEncodedText s_types = JsonEncodedText.Encode("types");
    private static readonly JsonEncodedText s_kind = JsonEncodedText.Encode("kind");
    private static readonly JsonEncodedText s_type = JsonEncodedText.Encode("type");

    internal static DragEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new DragEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_dataTransfer.EncodedUtf8Bytes))
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
            if (property.NameEquals(s_dropEffect.EncodedUtf8Bytes))
            {
                dataTransfer.DropEffect = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_effectAllowed.EncodedUtf8Bytes))
            {
                dataTransfer.EffectAllowed = property.Value.GetString();
            }
            else if (property.NameEquals(s_files.EncodedUtf8Bytes))
            {
                dataTransfer.Files = ReadStringArray(property.Value);
            }
            else if (property.NameEquals(s_items.EncodedUtf8Bytes))
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
            else if (property.NameEquals(s_types.EncodedUtf8Bytes))
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
            if (property.NameEquals(s_kind.EncodedUtf8Bytes))
            {
                item.Kind = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_type.EncodedUtf8Bytes))
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

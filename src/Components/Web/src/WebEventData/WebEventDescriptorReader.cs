// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

internal static class WebEventDescriptorReader
{
    private static readonly JsonEncodedText EventHandlerIdKey = JsonEncodedText.Encode("eventHandlerId");
    private static readonly JsonEncodedText EventNameKey = JsonEncodedText.Encode("eventName");
    private static readonly JsonEncodedText EventFieldInfoKey = JsonEncodedText.Encode("eventFieldInfo");
    private static readonly JsonEncodedText ComponentIdKey = JsonEncodedText.Encode("componentId");
    private static readonly JsonEncodedText FieldValueKey = JsonEncodedText.Encode("fieldValue");

    internal static WebEventDescriptor Read(JsonElement jsonElement)
    {
        var descriptor = new WebEventDescriptor();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(EventHandlerIdKey.EncodedUtf8Bytes))
            {
                descriptor.EventHandlerId = property.Value.GetUInt64();
            }
            else if (property.NameEquals(EventNameKey.EncodedUtf8Bytes))
            {
                descriptor.EventName = property.Value.GetString()!;
            }
            else if (property.NameEquals(EventFieldInfoKey.EncodedUtf8Bytes))
            {
                descriptor.EventFieldInfo = ReadEventFieldInfo(property.Value);
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return descriptor;
    }

    private static EventFieldInfo? ReadEventFieldInfo(JsonElement jsonElement)
    {
        if (jsonElement.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        var eventFieldInfo = new EventFieldInfo();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(ComponentIdKey.EncodedUtf8Bytes))
            {
                eventFieldInfo.ComponentId = property.Value.GetInt32();
            }
            else if (property.NameEquals(FieldValueKey.EncodedUtf8Bytes))
            {
                if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    eventFieldInfo.FieldValue = property.Value.GetBoolean();
                }
                else
                {
                    eventFieldInfo.FieldValue = property.Value.GetString()!;
                }
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return eventFieldInfo;
    }
}

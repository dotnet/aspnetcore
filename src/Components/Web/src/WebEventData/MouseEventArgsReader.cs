// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class MouseEventArgsReader
{
    private static readonly JsonEncodedText s_detail = JsonEncodedText.Encode("detail");
    private static readonly JsonEncodedText s_screenX = JsonEncodedText.Encode("screenX");
    private static readonly JsonEncodedText s_screenY = JsonEncodedText.Encode("screenY");
    private static readonly JsonEncodedText s_clientX = JsonEncodedText.Encode("clientX");
    private static readonly JsonEncodedText s_clientY = JsonEncodedText.Encode("clientY");
    private static readonly JsonEncodedText s_offsetX = JsonEncodedText.Encode("offsetX");
    private static readonly JsonEncodedText s_offsetY = JsonEncodedText.Encode("offsetY");
    private static readonly JsonEncodedText s_pageX = JsonEncodedText.Encode("pageX");
    private static readonly JsonEncodedText s_pageY = JsonEncodedText.Encode("pageY");
    private static readonly JsonEncodedText s_movementX = JsonEncodedText.Encode("movementX");
    private static readonly JsonEncodedText s_movementY = JsonEncodedText.Encode("movementY");
    private static readonly JsonEncodedText s_button = JsonEncodedText.Encode("button");
    private static readonly JsonEncodedText s_buttons = JsonEncodedText.Encode("buttons");
    private static readonly JsonEncodedText s_ctrlKey = JsonEncodedText.Encode("ctrlKey");
    private static readonly JsonEncodedText s_shiftKey = JsonEncodedText.Encode("shiftKey");
    private static readonly JsonEncodedText s_altKey = JsonEncodedText.Encode("altKey");
    private static readonly JsonEncodedText s_metaKey = JsonEncodedText.Encode("metaKey");
    private static readonly JsonEncodedText s_type = JsonEncodedText.Encode("type");

    internal static MouseEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new MouseEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            ReadProperty(eventArgs, property);
        }
        return eventArgs;
    }

    internal static void ReadProperty(MouseEventArgs eventArgs, JsonProperty property)
    {
        if (property.NameEquals(s_detail.EncodedUtf8Bytes))
        {
            eventArgs.Detail = property.Value.GetInt64();
        }
        else if (property.NameEquals(s_screenX.EncodedUtf8Bytes))
        {
            eventArgs.ScreenX = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_screenY.EncodedUtf8Bytes))
        {
            eventArgs.ScreenY = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_clientX.EncodedUtf8Bytes))
        {
            eventArgs.ClientX = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_clientY.EncodedUtf8Bytes))
        {
            eventArgs.ClientY = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_offsetX.EncodedUtf8Bytes))
        {
            eventArgs.OffsetX = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_offsetY.EncodedUtf8Bytes))
        {
            eventArgs.OffsetY = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_pageX.EncodedUtf8Bytes))
        {
            eventArgs.PageX = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_pageY.EncodedUtf8Bytes))
        {
            eventArgs.PageY = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_movementX.EncodedUtf8Bytes))
        {
            eventArgs.MovementX = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_movementY.EncodedUtf8Bytes))
        {
            eventArgs.MovementY = property.Value.GetDouble();
        }
        else if (property.NameEquals(s_button.EncodedUtf8Bytes))
        {
            eventArgs.Button = property.Value.GetInt64();
        }
        else if (property.NameEquals(s_buttons.EncodedUtf8Bytes))
        {
            eventArgs.Buttons = property.Value.GetInt64();
        }
        else if (property.NameEquals(s_ctrlKey.EncodedUtf8Bytes))
        {
            eventArgs.CtrlKey = property.Value.GetBoolean();
        }
        else if (property.NameEquals(s_shiftKey.EncodedUtf8Bytes))
        {
            eventArgs.ShiftKey = property.Value.GetBoolean();
        }
        else if (property.NameEquals(s_altKey.EncodedUtf8Bytes))
        {
            eventArgs.AltKey = property.Value.GetBoolean();
        }
        else if (property.NameEquals(s_metaKey.EncodedUtf8Bytes))
        {
            eventArgs.MetaKey = property.Value.GetBoolean();
        }
        else if (property.NameEquals(s_type.EncodedUtf8Bytes))
        {
            eventArgs.Type = property.Value.GetString()!;
        }
        else
        {
            throw new JsonException($"Unknown property {property.Name}");
        }
    }
}

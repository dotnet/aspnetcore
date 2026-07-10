// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class PointerEventArgsReader
{
    private static readonly JsonEncodedText s_pointerId = JsonEncodedText.Encode("pointerId");
    private static readonly JsonEncodedText s_width = JsonEncodedText.Encode("width");
    private static readonly JsonEncodedText s_height = JsonEncodedText.Encode("height");
    private static readonly JsonEncodedText s_pressure = JsonEncodedText.Encode("pressure");
    private static readonly JsonEncodedText s_tiltX = JsonEncodedText.Encode("tiltX");
    private static readonly JsonEncodedText s_tiltY = JsonEncodedText.Encode("tiltY");
    private static readonly JsonEncodedText s_pointerType = JsonEncodedText.Encode("pointerType");
    private static readonly JsonEncodedText s_isPrimary = JsonEncodedText.Encode("isPrimary");

    internal static PointerEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new PointerEventArgs();

        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_pointerId.EncodedUtf8Bytes))
            {
                eventArgs.PointerId = property.Value.GetInt64();
            }
            else if (property.NameEquals(s_width.EncodedUtf8Bytes))
            {
                eventArgs.Width = property.Value.GetSingle();
            }
            else if (property.NameEquals(s_height.EncodedUtf8Bytes))
            {
                eventArgs.Height = property.Value.GetSingle();
            }
            else if (property.NameEquals(s_pressure.EncodedUtf8Bytes))
            {
                eventArgs.Pressure = property.Value.GetSingle();
            }
            else if (property.NameEquals(s_tiltX.EncodedUtf8Bytes))
            {
                eventArgs.TiltX = property.Value.GetSingle();
            }
            else if (property.NameEquals(s_tiltY.EncodedUtf8Bytes))
            {
                eventArgs.TiltY = property.Value.GetSingle();
            }
            else if (property.NameEquals(s_pointerType.EncodedUtf8Bytes))
            {
                eventArgs.PointerType = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_isPrimary.EncodedUtf8Bytes))
            {
                eventArgs.IsPrimary = property.Value.GetBoolean();
            }
            else
            {
                MouseEventArgsReader.ReadProperty(eventArgs, property);
            }
        }

        return eventArgs;
    }
}

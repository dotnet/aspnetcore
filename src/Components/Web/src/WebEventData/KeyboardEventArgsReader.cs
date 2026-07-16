// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class KeyboardEventArgsReader
{
    private static readonly JsonEncodedText s_key = JsonEncodedText.Encode("key");
    private static readonly JsonEncodedText s_code = JsonEncodedText.Encode("code");
    private static readonly JsonEncodedText s_location = JsonEncodedText.Encode("location");
    private static readonly JsonEncodedText s_repeat = JsonEncodedText.Encode("repeat");
    private static readonly JsonEncodedText s_ctrlKey = JsonEncodedText.Encode("ctrlKey");
    private static readonly JsonEncodedText s_shiftKey = JsonEncodedText.Encode("shiftKey");
    private static readonly JsonEncodedText s_altKey = JsonEncodedText.Encode("altKey");
    private static readonly JsonEncodedText s_metaKey = JsonEncodedText.Encode("metaKey");
    private static readonly JsonEncodedText s_type = JsonEncodedText.Encode("type");
    private static readonly JsonEncodedText s_isComposing = JsonEncodedText.Encode("isComposing");

    internal static KeyboardEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new KeyboardEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_key.EncodedUtf8Bytes))
            {
                eventArgs.Key = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_code.EncodedUtf8Bytes))
            {
                eventArgs.Code = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_location.EncodedUtf8Bytes))
            {
                eventArgs.Location = property.Value.GetSingle()!;
            }
            else if (property.NameEquals(s_repeat.EncodedUtf8Bytes))
            {
                eventArgs.Repeat = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_ctrlKey.EncodedUtf8Bytes))
            {
                eventArgs.CtrlKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_altKey.EncodedUtf8Bytes))
            {
                eventArgs.AltKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_shiftKey.EncodedUtf8Bytes))
            {
                eventArgs.ShiftKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_metaKey.EncodedUtf8Bytes))
            {
                eventArgs.MetaKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_type.EncodedUtf8Bytes))
            {
                eventArgs.Type = property.Value.GetString()!;
            }
            else if (property.NameEquals(s_isComposing.EncodedUtf8Bytes))
            {
                eventArgs.IsComposing = property.Value.GetBoolean();
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }
        return eventArgs;
    }
}

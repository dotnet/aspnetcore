// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class KeyboardEventArgsReader
{
    private static readonly JsonEncodedText Key = JsonEncodedText.Encode("key");
    private static readonly JsonEncodedText Code = JsonEncodedText.Encode("code");
    private static readonly JsonEncodedText Location = JsonEncodedText.Encode("location");
    private static readonly JsonEncodedText Repeat = JsonEncodedText.Encode("repeat");
    private static readonly JsonEncodedText CtrlKey = JsonEncodedText.Encode("ctrlKey");
    private static readonly JsonEncodedText ShiftKey = JsonEncodedText.Encode("shiftKey");
    private static readonly JsonEncodedText AltKey = JsonEncodedText.Encode("altKey");
    private static readonly JsonEncodedText MetaKey = JsonEncodedText.Encode("metaKey");
    private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");
    private static readonly JsonEncodedText IsComposing = JsonEncodedText.Encode("isComposing");

    internal static KeyboardEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new KeyboardEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(Key.EncodedUtf8Bytes))
            {
                eventArgs.Key = property.Value.GetString()!;
            }
            else if (property.NameEquals(Code.EncodedUtf8Bytes))
            {
                eventArgs.Code = property.Value.GetString()!;
            }
            else if (property.NameEquals(Location.EncodedUtf8Bytes))
            {
                eventArgs.Location = property.Value.GetSingle()!;
            }
            else if (property.NameEquals(Repeat.EncodedUtf8Bytes))
            {
                eventArgs.Repeat = property.Value.GetBoolean();
            }
            else if (property.NameEquals(CtrlKey.EncodedUtf8Bytes))
            {
                eventArgs.CtrlKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(AltKey.EncodedUtf8Bytes))
            {
                eventArgs.AltKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(ShiftKey.EncodedUtf8Bytes))
            {
                eventArgs.ShiftKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(MetaKey.EncodedUtf8Bytes))
            {
                eventArgs.MetaKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(Type.EncodedUtf8Bytes))
            {
                eventArgs.Type = property.Value.GetString()!;
            }
            else if (property.NameEquals(IsComposing.EncodedUtf8Bytes))
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

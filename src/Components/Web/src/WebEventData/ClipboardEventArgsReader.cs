// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class ClipboardEventArgsReader
{
    private static readonly JsonEncodedText s_typeKey = JsonEncodedText.Encode("type");

    internal static ClipboardEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new ClipboardEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_typeKey.EncodedUtf8Bytes))
            {
                eventArgs.Type = property.Value.GetString()!;
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return eventArgs;
    }
}

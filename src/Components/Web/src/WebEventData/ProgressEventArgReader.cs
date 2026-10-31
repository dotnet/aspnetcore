// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class ProgressEventArgsReader
{
    private static readonly JsonEncodedText LengthComputable = JsonEncodedText.Encode("lengthComputable");
    private static readonly JsonEncodedText Loaded = JsonEncodedText.Encode("loaded");
    private static readonly JsonEncodedText Total = JsonEncodedText.Encode("total");
    private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");

    internal static ProgressEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new ProgressEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(LengthComputable.EncodedUtf8Bytes))
            {
                eventArgs.LengthComputable = property.Value.GetBoolean();
            }
            else if (property.NameEquals(Loaded.EncodedUtf8Bytes))
            {
                eventArgs.Loaded = property.Value.GetInt64();
            }
            else if (property.NameEquals(Total.EncodedUtf8Bytes))
            {
                eventArgs.Total = property.Value.GetInt64();
            }
            else if (property.NameEquals(Type.EncodedUtf8Bytes))
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

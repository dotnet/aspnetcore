// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class ErrorEventArgsReader
{
    private static readonly JsonEncodedText Message = JsonEncodedText.Encode("message");
    private static readonly JsonEncodedText Colno = JsonEncodedText.Encode("colno");
    private static readonly JsonEncodedText Filename = JsonEncodedText.Encode("filename");
    private static readonly JsonEncodedText Lineno = JsonEncodedText.Encode("lineno");
    private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");

    internal static ErrorEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new ErrorEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(Message.EncodedUtf8Bytes))
            {
                eventArgs.Message = property.Value.GetString()!;
            }
            else if (property.NameEquals(Colno.EncodedUtf8Bytes))
            {
                eventArgs.Colno = property.Value.GetInt32();
            }
            else if (property.NameEquals(Filename.EncodedUtf8Bytes))
            {
                eventArgs.Filename = property.Value.GetString()!;
            }
            else if (property.NameEquals(Lineno.EncodedUtf8Bytes))
            {
                eventArgs.Lineno = property.Value.GetInt32();
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

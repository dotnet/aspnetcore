// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

internal sealed class WebIdlEnumJsonConverter<T> : JsonConverter<T> where T : struct, IWebIdlEnum<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string for {typeof(T).Name}, but got {reader.TokenType}.");
        }

        return T.Create(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // default(T) has no string. Sending null would reach the browser as the string "null".
        if (value.Value is null)
        {
            throw new JsonException($"The default value of {typeof(T).Name} cannot be sent to the browser.");
        }

        writer.WriteStringValue(value.Value);
    }
}

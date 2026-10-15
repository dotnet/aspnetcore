// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class ByteArrayJsonConverter : JsonConverter<byte[]>
{
    // Unique identifier for serializing byte arrays.
    private int _byteArrayId;

    internal static readonly JsonEncodedText ByteArrayRefKey = JsonEncodedText.Encode("__byte[]");

    public ByteArrayJsonConverter(JSRuntime jSRuntime)
    {
        JSRuntime = jSRuntime;
    }

    public JSRuntime JSRuntime { get; }

    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // For back-compat reasons, try reading the input as a base-64 encoded string at first.
        if (reader.TokenType == JsonTokenType.String && reader.TryGetBytesFromBase64(out var bytes))
        {
            return bytes;
        }

        if (JSRuntime.ByteArraysToBeRevived.Count == 0)
        {
            throw new JsonException("JSON serialization is attempting to deserialize an unexpected byte array.");
        }

        int byteArrayRef;

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Unexpected JSON Token {reader.TokenType}, expected 'StartObject'.");
        }

        if (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (!reader.ValueTextEquals(ByteArrayRefKey.EncodedUtf8Bytes))
            {
                throw new JsonException($"Unexpected JSON Property {reader.GetString()}.");
            }
            else if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Unexpected JSON Token {reader.TokenType}, expected 'Number'.");
            }
            else if (!reader.TryGetInt32(out byteArrayRef))
            {
                throw new JsonException($"Unexpected number, expected 32-bit integer.");
            }
        }
        else
        {
            throw new JsonException($"Unexpected JSON Token {reader.TokenType}, expected 'PropertyName'.");
        }

        if (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException($"Unexpected JSON Token {reader.TokenType}, expected 'EndObject'.");
        }

        if (byteArrayRef >= JSRuntime.ByteArraysToBeRevived.Count || byteArrayRef < 0)
        {
            throw new JsonException($"Byte array {byteArrayRef} not found.");
        }

        var byteArray = JSRuntime.ByteArraysToBeRevived.Buffer[byteArrayRef];
        return byteArray;
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        var id = ++_byteArrayId;

        JSRuntime.SendByteArray(id, value);

        writer.WriteStartObject();
        writer.WriteNumber(ByteArrayRefKey, id);
        writer.WriteEndObject();
    }
}

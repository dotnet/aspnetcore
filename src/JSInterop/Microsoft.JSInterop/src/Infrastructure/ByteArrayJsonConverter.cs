// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class ByteArrayJsonConverter : JsonConverter<byte[]>
    {
        private static readonly JsonEncodedText ByteArrayRefKey = JsonEncodedText.Encode("__byte[]");
        private readonly JSRuntime _jsRuntime;

        public ByteArrayJsonConverter(JSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(byte[]);

        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (_jsRuntime.ByteArraysToDeserialize is null)
            {
                throw new JsonException($"ByteArraysToDeserialize not set.");
            }

            long? byteArrayIndex = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (byteArrayIndex is null && reader.ValueTextEquals(ByteArrayRefKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        byteArrayIndex = reader.GetInt64();
                    }
                    else
                    {
                        throw new JsonException($"Unexcepted JSON property {reader.GetString()}.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexcepted JSON Token {reader.TokenType}.");
                }
            }

            if (byteArrayIndex is null)
            {
                throw new JsonException($"Required property {ByteArrayRefKey} not found.");
            }

            if (byteArrayIndex >= _jsRuntime.ByteArraysToDeserialize.Length || byteArrayIndex < 0)
            {
                throw new JsonException($"Byte array {byteArrayIndex} not found.");
            }

            var value = _jsRuntime.ByteArraysToDeserialize[byteArrayIndex.Value];
            return value;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            var id = _jsRuntime.ByteArraysToSerialize.Count;

            _jsRuntime.ByteArraysToSerialize.Add(value);

            writer.WriteStartObject();
            writer.WriteNumber(ByteArrayRefKey, id);
            writer.WriteEndObject();
        }
    }
}

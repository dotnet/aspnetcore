// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
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
            if (JSRuntime.ByteArraysToBeRevived.Count == 0)
            {
                throw new JsonException("ByteArraysToBeRevived is empty.");
            }

            int? byteArrayRef = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (byteArrayRef is null && reader.ValueTextEquals(ByteArrayRefKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        byteArrayRef = reader.GetInt32();
                    }
                    else
                    {
                        throw new JsonException($"Unexpected JSON property {reader.GetString()}.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexpected JSON Token {reader.TokenType}.");
                }
            }

            if (byteArrayRef is null)
            {
                throw new JsonException($"Required property {ByteArrayRefKey} not found.");
            }

            if (byteArrayRef >= JSRuntime.ByteArraysToBeRevived.Count || byteArrayRef < 0)
            {
                throw new JsonException($"Byte array {byteArrayRef} not found.");
            }

            var byteArray = JSRuntime.ByteArraysToBeRevived.Buffer[byteArrayRef.Value];
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

        private struct ByteArrayRef
        {
            [JsonPropertyName("__byte[]")]
            public int? Id { get; set; }
        }
    }
}

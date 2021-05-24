// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class ByteArrayJsonConverter : JsonConverter<byte[]>
    {
        private byte[][]? _byteArraysToDeserialize;
        internal static readonly JsonEncodedText ByteArrayRefKey = JsonEncodedText.Encode("__byte[]");

        /// <summary>
        /// Contains the byte array(s) being serialized.
        /// </summary>
        internal readonly List<byte[]> ByteArraysToSerialize = new();

        /// <summary>
        /// Sets the byte array(s) being deserialized.
        /// </summary>
        internal byte[][]? ByteArraysToDeserialize
        {
            set
            {
                if (_byteArraysToDeserialize is not null && value is not null)
                {
                    throw new JsonException("Unable to deserialize arguments, previous deserialization is incomplete.");
                }

                _byteArraysToDeserialize = value;
            }
        }

        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(byte[]);

        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (_byteArraysToDeserialize is null)
            {
                throw new JsonException("ByteArraysToDeserialize not set.");
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
                        throw new JsonException($"Unexpected JSON property {reader.GetString()}.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexpected JSON Token {reader.TokenType}.");
                }
            }

            if (byteArrayIndex is null)
            {
                throw new JsonException($"Required property {ByteArrayRefKey} not found.");
            }

            if (byteArrayIndex >= _byteArraysToDeserialize.Length || byteArrayIndex < 0)
            {
                throw new JsonException($"Byte array {byteArrayIndex} not found.");
            }

            var value = _byteArraysToDeserialize[byteArrayIndex.Value];
            return value;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            var id = ByteArraysToSerialize.Count;

            ByteArraysToSerialize.Add(value);

            writer.WriteStartObject();
            writer.WriteNumber(ByteArrayRefKey, id);
            writer.WriteEndObject();
        }
    }
}

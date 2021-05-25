// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class ByteArrayJsonConverter : JsonConverter<byte[]>
    {
        /// Atomically incrementing unique identifier for serializing byte arrays.
        private static long _byteArrayId;

        internal static readonly JsonEncodedText ByteArrayRefKey = JsonEncodedText.Encode("__byte[]");

        public ByteArrayJsonConverter(JSRuntime jSRuntime)
        {
            JSRuntime = jSRuntime;
        }

        public JSRuntime JSRuntime { get; }

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == typeof(byte[]) || typeToConvert == typeof(ReadOnlySequence<byte>);

        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JSRuntime.ByteArraysToBeRevived.Count == 0)
            {
                throw new JsonException("ByteArraysToBeRevived is empty.");
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

            if (byteArrayIndex >= JSRuntime.ByteArraysToBeRevived.Count || byteArrayIndex < 0)
            {
                throw new JsonException($"Byte array {byteArrayIndex} not found.");
            }

            var byteArray = JSRuntime.ByteArraysToBeRevived.Buffer[byteArrayIndex.Value];
            if (byteArray is null || byteArray.Length == 0)
            {
                return Array.Empty<byte>();
            }

            // We must copy over the byte array to ensure the data is preserved after we
            // clear the JSRuntime.ByteArraysToBeRevived buffer.
            var result = new byte[byteArray.Length];
            Array.Copy(byteArray, result, byteArray.Length);
            return result;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            var id = Interlocked.Increment(ref _byteArrayId);

            JSRuntime.SupplyByteArray(id, value);

            writer.WriteStartObject();
            writer.WriteNumber(ByteArrayRefKey, id);
            writer.WriteEndObject();
        }
    }
}

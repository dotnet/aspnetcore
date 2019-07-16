// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop
{
    internal sealed class DotNetObjectReferenceJsonConverter<TValue> : JsonConverter<DotNetObjectRef<TValue>> where TValue : class
    {
        private static JsonEncodedText DotNetObjectRefKey => DotNetDispatcher.DotNetObjectRefKey;

        public override DotNetObjectRef<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.Read())
            {
                throw new InvalidDataException("Invalid DotNetObjectRef JSON.");
            }

            if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals(DotNetObjectRefKey.EncodedUtf8Bytes))
            {
                throw new InvalidDataException("Invalid DotNetObjectRef JSON.");
            }

            if (!reader.Read())
            {
                throw new InvalidDataException("Invalid DotNetObjectRef JSON.");
            }

            var dotNetObjectId = reader.GetInt64();

            if (!reader.Read())
            {
                // We need to read all the data that was given to us.
                throw new InvalidDataException("Invalid DotNetObjectRef JSON.");
            }

            var value = (TValue)DotNetObjectRefManager.Current.FindDotNetObject(dotNetObjectId);
            return new DotNetObjectRef<TValue>(dotNetObjectId, value);
        }

        public override void Write(Utf8JsonWriter writer, DotNetObjectRef<TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(DotNetObjectRefKey, value.ObjectId);
            writer.WriteEndObject();
        }
    }
}

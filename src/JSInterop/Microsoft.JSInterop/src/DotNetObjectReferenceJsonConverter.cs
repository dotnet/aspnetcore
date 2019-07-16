// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop
{
    internal sealed class DotNetObjectReferenceJsonConverter<TValue> : JsonConverter<DotNetObjectRef<TValue>> where TValue : class
    {
        private static JsonEncodedText DotNetObjectRefKey => DotNetDispatcher.DotNetObjectRefKey;

        public override DotNetObjectRef<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long dotNetObjectId = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(DotNetObjectRefKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        dotNetObjectId = reader.GetInt64();
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

            if (dotNetObjectId is 0)
            {
                throw new JsonException($"Required property {DotNetObjectRefKey} not found.");
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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class DotNetObjectReferenceJsonConverter<TValue> : JsonConverter<DotNetObjectReference<TValue>> where TValue : class
    {
        public DotNetObjectReferenceJsonConverter(JSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
        }

        private static JsonEncodedText DotNetObjectRefKey => DotNetDispatcher.DotNetObjectRefKey;

        public JSRuntime JSRuntime { get; }

        public override DotNetObjectReference<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long dotNetObjectId = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (dotNetObjectId == 0 && reader.ValueTextEquals(DotNetObjectRefKey.EncodedUtf8Bytes))
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

            var value = (DotNetObjectReference<TValue>)JSRuntime.GetObjectReference(dotNetObjectId);
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DotNetObjectReference<TValue> value, JsonSerializerOptions options)
        {
            var objectId = JSRuntime.TrackObjectReference<TValue>(value);

            writer.WriteStartObject();
            writer.WriteNumber(DotNetObjectRefKey, objectId);
            writer.WriteEndObject();
        }
    }
}

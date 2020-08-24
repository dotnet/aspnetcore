// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class JSObjectReferenceJsonConverter<TJSObjectReference>
        : JsonConverter<TJSObjectReference> where TJSObjectReference : JSObjectReference
    {
        private readonly Func<long, TJSObjectReference> _jsObjectReferenceFactory;

        public JSObjectReferenceJsonConverter(Func<long, TJSObjectReference> jsObjectReferenceFactory)
        {
            _jsObjectReferenceFactory = jsObjectReferenceFactory;
        }

        public override TJSObjectReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long id = -1;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (id == -1 && reader.ValueTextEquals(JSObjectReference.IdKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        id = reader.GetInt64();
                    }
                    else
                    {
                        throw new JsonException($"Unexcepted JSON property {reader.GetString()}.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexcepted JSON token {reader.TokenType}");
                }
            }

            if (id == -1)
            {
                throw new JsonException($"Required property {JSObjectReference.IdKey} not found.");
            }

            return _jsObjectReferenceFactory(id);
        }

        public override void Write(Utf8JsonWriter writer, TJSObjectReference value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(JSObjectReference.IdKey, value.Id);
            writer.WriteEndObject();
        }
    }
}

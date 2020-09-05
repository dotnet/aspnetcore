// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class JSObjectReferenceJsonConverter<TInterface, TImplementation> : JsonConverter<TInterface>
        where TInterface : class, IJSObjectReference
        where TImplementation : JSObjectReference, TInterface
    {
        private static readonly JsonEncodedText _idKey = JsonEncodedText.Encode("__jsObjectId");

        private readonly Func<long, TImplementation> _jsObjectReferenceFactory;

        public JSObjectReferenceJsonConverter(Func<long, TImplementation> jsObjectReferenceFactory)
        {
            _jsObjectReferenceFactory = jsObjectReferenceFactory;
        }

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(TInterface) || typeToConvert == typeof(TImplementation);

        public override TInterface? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long id = -1;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (id == -1 && reader.ValueTextEquals(_idKey.EncodedUtf8Bytes))
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
                throw new JsonException($"Required property {_idKey} not found.");
            }

            return _jsObjectReferenceFactory(id);
        }

        public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(_idKey, ((TImplementation)value).Id);
            writer.WriteEndObject();
        }
    }
}

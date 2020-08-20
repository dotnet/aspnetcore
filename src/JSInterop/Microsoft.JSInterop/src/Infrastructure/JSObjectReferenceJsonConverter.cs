// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class JSObjectReferenceJsonConverter : JsonConverter<JSObjectReference>
    {
        private readonly JSRuntime _jsRuntime;

        public JSObjectReferenceJsonConverter(JSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override JSObjectReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long id = -1;

            while (reader.Read())
            {
                if (id < 0 && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(JSObjectReference.IdKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        id = reader.GetInt64();
                    }
                }
            }

            if (id < 0)
            {
                throw new JsonException($"Required property {JSObjectReference.IdKey} not found.");
            }

            return new JSObjectReference(_jsRuntime, id);
        }

        public override void Write(Utf8JsonWriter writer, JSObjectReference value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(JSObjectReference.IdKey, value.Id);
            writer.WriteEndObject();
        }
    }
}

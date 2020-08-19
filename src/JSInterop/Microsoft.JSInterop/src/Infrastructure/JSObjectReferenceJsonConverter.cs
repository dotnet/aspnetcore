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
            long jsObjectId = -1;

            while (reader.Read())
            {
                if (jsObjectId < 0 && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(JSObjectReference.JSObjectIdKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        jsObjectId = reader.GetInt64();
                    }
                }
            }

            if (jsObjectId < 0)
            {
                throw new JsonException($"Required property {JSObjectReference.JSObjectIdKey} not found.");
            }

            return new JSObjectReference(_jsRuntime, jsObjectId);
        }

        public override void Write(Utf8JsonWriter writer, JSObjectReference value, JsonSerializerOptions options)
        {
            // TODO: Decide if passing JSObjectReferences back into functions should be allowed.
            throw new NotImplementedException();
        }
    }
}

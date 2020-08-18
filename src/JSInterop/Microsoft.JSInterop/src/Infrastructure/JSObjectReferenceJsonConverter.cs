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
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(JSObjectReference.JSObjectIdKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        var jsObjectId = reader.GetInt64();
                        return new JSObjectReference(_jsRuntime, jsObjectId);
                    }
                }
            }

            throw new JsonException($"Required property {JSObjectReference.JSObjectIdKey} not found.");
        }

        public override void Write(Utf8JsonWriter writer, JSObjectReference value, JsonSerializerOptions options)
        {
            // TODO: Decide if passing JSObjectReferences back into functions should be allowed.
            throw new NotImplementedException();
        }
    }
}

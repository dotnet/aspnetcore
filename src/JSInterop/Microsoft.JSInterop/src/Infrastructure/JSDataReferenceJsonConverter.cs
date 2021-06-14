// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class JSDataReferenceJsonConverter : JsonConverter<IJSDataReference>
    {
        private static readonly JsonEncodedText _jsDataReferenceLengthKey = JsonEncodedText.Encode("__jsDataReferenceLength");

        private readonly JSRuntime _jsRuntime;

        public JSDataReferenceJsonConverter(JSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(IJSDataReference) || typeToConvert == typeof(JSDataReference);

        public override IJSDataReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long? id = null;
            long? length = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (id is null && reader.ValueTextEquals(JSObjectReferenceJsonWorker.JSObjectIdKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        id = reader.GetInt64();
                    }
                    else if (length is null && reader.ValueTextEquals(_jsDataReferenceLengthKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        length = reader.GetInt64();
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

            if (!id.HasValue)
            {
                throw new JsonException($"Required property {JSObjectReferenceJsonWorker.JSObjectIdKey} not found.");
            }

            if (!length.HasValue)
            {
                throw new JsonException($"Required property {_jsDataReferenceLengthKey} not found.");
            }

            return new JSDataReference(_jsRuntime, id.Value, length.Value);
        }

        public override void Write(Utf8JsonWriter writer, IJSDataReference value, JsonSerializerOptions options)
        {
            JSObjectReferenceJsonWorker.WriteJSObjectReference(writer, (JSDataReference)value);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class JSStreamReferenceJsonConverter : JsonConverter<IJSStreamReference>
    {
        private static readonly JsonEncodedText _jsStreamReferenceLengthKey = JsonEncodedText.Encode("__jsStreamReferenceLength");

        private readonly JSRuntime _jsRuntime;

        public JSStreamReferenceJsonConverter(JSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(IJSStreamReference) || typeToConvert == typeof(JSStreamReference);

        public override IJSStreamReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                    else if (length is null && reader.ValueTextEquals(_jsStreamReferenceLengthKey.EncodedUtf8Bytes))
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
                throw new JsonException($"Required property {_jsStreamReferenceLengthKey} not found.");
            }

            return new JSStreamReference(_jsRuntime, id.Value, length.Value);
        }

        public override void Write(Utf8JsonWriter writer, IJSStreamReference value, JsonSerializerOptions options)
        {
            JSObjectReferenceJsonWorker.WriteJSObjectReference(writer, (JSStreamReference)value);
        }
    }
}

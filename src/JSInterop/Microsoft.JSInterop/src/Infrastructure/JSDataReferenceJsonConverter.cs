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
        private readonly JSRuntime _jsRuntime;

        public JSDataReferenceJsonConverter(JSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(IJSDataReference) || typeToConvert == typeof(JSDataReference);

        public override IJSDataReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var deserializedValues = JSObjectReferenceJsonWorker.ReadJSObjectReference(ref reader);

            if (!deserializedValues.Length.HasValue)
            {
                throw new JsonException($"Required property __jsDataReferenceLength not found.");
            }

            return new JSDataReference(_jsRuntime, deserializedValues.Id, deserializedValues.Length.Value);
        }

        public override void Write(Utf8JsonWriter writer, IJSDataReference value, JsonSerializerOptions options)
        {
            JSObjectReferenceJsonWorker.WriteJSObjectReference(writer, (JSDataReference)value);
        }
    }
}

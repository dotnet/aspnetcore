// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.WebAssembly
{
    internal sealed class WebAssemblyJSObjectReferenceJsonConverter : JsonConverter<IJSObjectReference>
    {
        private readonly WebAssemblyJSRuntime _jsRuntime;

        public WebAssemblyJSObjectReferenceJsonConverter(WebAssemblyJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(WebAssemblyJSObjectReference) ||
                typeToConvert == typeof(IJSObjectReference) ||
                typeToConvert == typeof(IJSInProcessObjectReference) ||
                typeToConvert == typeof(IJSUnmarshalledObjectReference) ||
                typeToConvert == typeof(JSObjectReference);
        }

        public override IJSObjectReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var id = JSObjectReferenceJsonWorker.ReadIdentifier(ref reader);
            return new WebAssemblyJSObjectReference(_jsRuntime, id);
        }

        public override void Write(Utf8JsonWriter writer, IJSObjectReference value, JsonSerializerOptions options)
        {
            JSObjectReferenceJsonWorker.Write(writer, ((WebAssemblyJSObjectReference)value).Id);
        }
    }
}

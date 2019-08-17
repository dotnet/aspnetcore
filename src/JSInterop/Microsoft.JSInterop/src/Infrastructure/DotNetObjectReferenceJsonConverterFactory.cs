// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class DotNetObjectReferenceJsonConverterFactory : JsonConverterFactory
    {
        public DotNetObjectReferenceJsonConverterFactory(JSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
        }

        public JSRuntime JSRuntime { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DotNetObjectReference<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions jsonSerializerOptions)
        {
            // System.Text.Json handles caching the converters per type on our behalf. No caching is required here.
            var instanceType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(DotNetObjectReferenceJsonConverter<>).MakeGenericType(instanceType);

            return (JsonConverter)Activator.CreateInstance(converterType, JSRuntime);
        }
    }
}

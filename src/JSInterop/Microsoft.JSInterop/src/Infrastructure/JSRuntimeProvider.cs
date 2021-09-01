// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class JSRuntimeProvider : JsonConverter<JSRuntime>
    {
        public JSRuntimeProvider(JSRuntime jsRuntime) => JSRuntime = jsRuntime;

        public JSRuntime JSRuntime { get; }

        public override bool CanConvert(Type typeToConvert) => false;

        public override JSRuntime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, JSRuntime value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        internal static JSRuntime GetJSRuntime(JsonSerializerOptions options)
        {
            for (var i = 0; i < options.Converters.Count; i++)
            {
                if (options.Converters[i] is JSRuntimeProvider jsRuntimeProvider)
                {
                    return jsRuntimeProvider.JSRuntime;
                }
            }

            throw new InvalidOperationException("Unable to find JSRuntimeProvider in the configured options.");
        }
    }
}

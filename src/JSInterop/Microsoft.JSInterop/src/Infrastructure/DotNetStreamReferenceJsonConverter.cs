// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure
{
    internal sealed class DotNetStreamReferenceJsonConverter : JsonConverter<DotNetStreamReference>
    {
        public DotNetStreamReferenceJsonConverter(JSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
        }

        private readonly static JsonEncodedText DotNetStreamRefKey = JsonEncodedText.Encode("__dotNetStream");

        public JSRuntime JSRuntime { get; }

        public override DotNetStreamReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException($"{nameof(DotNetStreamReference)} cannot be supplied from JavaScript to .NET because the stream is released after being sent.");

        public override void Write(Utf8JsonWriter writer, DotNetStreamReference value, JsonSerializerOptions options)
        {
            // We only serialize a DotNetStreamReference using this converter when we're supplying that info
            // to JS. We want to transmit the stream immediately as part of this process, so that the .NET side
            // doesn't have to hold onto the stream waiting for JS to request it. If a developer doesn't really
            // want to send the data, they shouldn't include the DotNetStreamReference in the object graph
            // they are sending to the JS side.
            var id = JSRuntime.BeginTransmittingStream(value);
            writer.WriteStartObject();
            writer.WriteNumber(DotNetStreamRefKey, id);
            writer.WriteEndObject();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class DotNetStreamReferenceJsonConverter : JsonConverter<DotNetStreamReference>
{
    private static readonly JsonEncodedText DotNetStreamRefKey = JsonEncodedText.Encode("__dotNetStream");

    public DotNetStreamReferenceJsonConverter(JSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime;
    }

    public JSRuntime JSRuntime { get; }

    public override DotNetStreamReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException($"{nameof(DotNetStreamReference)} cannot be supplied from JavaScript to .NET because the stream contents have already been transferred.");

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

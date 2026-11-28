// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class CancellationTokenJsonConverter : JsonConverter<CancellationToken>
{
    private readonly DotNetObjectReferenceJsonConverter<JSCancellationTokenReference> _referenceConverter;
    private readonly DotNetObjectReferenceJsonConverter<CancellationTokenObserver> _observerConverter;
    private readonly JSRuntime _jsRuntime;

    public CancellationTokenJsonConverter(JSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _referenceConverter = new(jsRuntime);
        _observerConverter = new(jsRuntime);
    }

    public override CancellationToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var reference = _referenceConverter.Read(ref reader, typeof(DotNetObjectReference<JSCancellationTokenReference>), options);
        return reference.Value.Token;
    }

    public override void Write(Utf8JsonWriter writer, CancellationToken value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("__dotNetCancellationToken");
        if (value.CanBeCanceled && !value.IsCancellationRequested)
        {
            var observer = _jsRuntime.CreateCancellationTokenObserver(value);
            _observerConverter.Write(writer, observer.Reference, options);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteBoolean("isCancellationRequested", value.IsCancellationRequested);
        writer.WriteEndObject();
    }
}

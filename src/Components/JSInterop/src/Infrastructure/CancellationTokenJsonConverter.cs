// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class CancellationTokenJsonConverter : JsonConverter<CancellationToken>
{
    private static readonly JsonEncodedText AbortSignalKey = JsonEncodedText.Encode("__abortSignal");

    private readonly DotNetObjectReferenceJsonConverter<CancellationTokenObserver> _observerConverter;
    private readonly JSRuntime _jsRuntime;

    public CancellationTokenJsonConverter(JSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _observerConverter = new(jsRuntime);
    }

    public override CancellationToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject
            || !reader.Read()
            || reader.TokenType != JsonTokenType.PropertyName
            || !reader.ValueTextEquals(AbortSignalKey.EncodedUtf8Bytes)
            || !reader.Read()
            || !reader.TryGetInt64(out var abortSignalId)
            || abortSignalId < 0
            || !reader.Read()
            || reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException($"A {nameof(CancellationToken)} can only be received from a JavaScript AbortSignal.");
        }

        return _jsRuntime.GetAbortSignalToken(abortSignalId);
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

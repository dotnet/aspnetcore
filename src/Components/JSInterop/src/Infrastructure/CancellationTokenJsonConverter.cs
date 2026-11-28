// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class CancellationTokenJsonConverter : JsonConverter<CancellationToken>
{
    private static readonly JsonEncodedText AbortSignalKey = JsonEncodedText.Encode("__abortSignal");
    private static readonly JsonEncodedText AbortedKey = JsonEncodedText.Encode("aborted");

    private readonly JSRuntime _jsRuntime;

    public CancellationTokenJsonConverter(JSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override CancellationToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject
            || !reader.Read()
            || reader.TokenType != JsonTokenType.PropertyName
            || !reader.ValueTextEquals(AbortSignalKey.EncodedUtf8Bytes)
            || !reader.Read()
            || !reader.TryGetInt64(out var abortSignalId)
            || abortSignalId < 1
            || !reader.Read()
            || reader.TokenType != JsonTokenType.PropertyName
            || !reader.ValueTextEquals(AbortedKey.EncodedUtf8Bytes)
            || !reader.Read()
            || !TryGetBoolean(ref reader, out var aborted)
            || !reader.Read()
            || reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException($"A {nameof(CancellationToken)} can only be received from a JavaScript AbortSignal.");
        }

        return _jsRuntime.GetAbortSignalToken(abortSignalId, aborted);
    }

    private static bool TryGetBoolean(ref Utf8JsonReader reader, out bool value)
    {
        value = reader.TokenType == JsonTokenType.True;
        return value || reader.TokenType == JsonTokenType.False;
    }

    public override void Write(Utf8JsonWriter writer, CancellationToken value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        // Read once: a cancellation after this point reaches JS as an abort for the id.
        var isCancellationRequested = value.IsCancellationRequested;
        writer.WritePropertyName("__dotNetCancellationToken");
        if (value.CanBeCanceled && !isCancellationRequested)
        {
            writer.WriteNumberValue(_jsRuntime.AddSerializingCancellationToken(value));
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WriteBoolean("isCancellationRequested", isCancellationRequested);
        writer.WriteEndObject();
    }
}

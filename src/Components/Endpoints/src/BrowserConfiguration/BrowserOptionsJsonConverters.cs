// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

// Serializes a nullable <see cref="TimeSpan"/> as whole milliseconds, matching the
// numeric shape expected by the Blazor JS runtime (e.g. retryIntervalMilliseconds).
internal sealed class TimeSpanMillisecondsJsonConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType is JsonTokenType.Null ? null : TimeSpan.FromMilliseconds(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value is { } timeSpan)
        {
            writer.WriteNumberValue((long)timeSpan.TotalMilliseconds);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

// Serializes a positive nullable boolean (e.g. PreserveDom) as its negated JS form
// (e.g. disableDomPreservation), keeping the public API idiomatic while the wire
// stays aligned with the JS runtime.
internal sealed class NegatedBooleanJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType is JsonTokenType.Null ? null : !reader.GetBoolean();

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is { } boolean)
        {
            writer.WriteBooleanValue(!boolean);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

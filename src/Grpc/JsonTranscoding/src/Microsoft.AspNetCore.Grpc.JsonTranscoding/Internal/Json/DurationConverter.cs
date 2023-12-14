// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class DurationConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public DurationConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new InvalidOperationException("Expected string value for Duration.");
        }

        var (seconds, nanos) = Legacy.ParseDuration(reader.GetString()!);

        var message = new TMessage();
        if (message is Duration duration)
        {
            duration.Seconds = seconds;
            duration.Nanos = nanos;
        }
        else
        {
            message.Descriptor.Fields[Duration.SecondsFieldNumber].Accessor.SetValue(message, seconds);
            message.Descriptor.Fields[Duration.NanosFieldNumber].Accessor.SetValue(message, nanos);
        }
        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        int nanos;
        long seconds;
        if (value is Duration duration)
        {
            nanos = duration.Nanos;
            seconds = duration.Seconds;
        }
        else
        {
            nanos = (int)value.Descriptor.Fields[Duration.NanosFieldNumber].Accessor.GetValue(value);
            seconds = (long)value.Descriptor.Fields[Duration.SecondsFieldNumber].Accessor.GetValue(value);
        }

        var text = Legacy.GetDurationText(nanos, seconds);
        writer.WriteStringValue(text);
    }
}

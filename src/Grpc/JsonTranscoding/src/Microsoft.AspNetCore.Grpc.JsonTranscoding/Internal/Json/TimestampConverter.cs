// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class TimestampConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public TimestampConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new InvalidOperationException("Expected string value for Timestamp.");
        }
        var (seconds, nanos) = Legacy.ParseTimestamp(reader.GetString()!);

        var message = new TMessage();
        if (message is Timestamp timestamp)
        {
            timestamp.Seconds = seconds;
            timestamp.Nanos = nanos;
        }
        else
        {
            message.Descriptor.Fields[Timestamp.SecondsFieldNumber].Accessor.SetValue(message, seconds);
            message.Descriptor.Fields[Timestamp.NanosFieldNumber].Accessor.SetValue(message, nanos);
        }
        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        int nanos;
        long seconds;
        if (value is Timestamp timestamp)
        {
            nanos = timestamp.Nanos;
            seconds = timestamp.Seconds;
        }
        else
        {
            nanos = (int)value.Descriptor.Fields[Timestamp.NanosFieldNumber].Accessor.GetValue(value);
            seconds = (long)value.Descriptor.Fields[Timestamp.SecondsFieldNumber].Accessor.GetValue(value);
        }

        var text = Legacy.GetTimestampText(nanos, seconds);
        writer.WriteStringValue(text);
    }
}

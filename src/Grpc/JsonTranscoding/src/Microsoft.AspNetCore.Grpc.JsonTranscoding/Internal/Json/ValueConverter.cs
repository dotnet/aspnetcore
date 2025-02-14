// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class ValueConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public override bool HandleNull => true;

    public ValueConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var message = new TMessage();
        var fields = message.Descriptor.Fields;
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                {
                    var field = fields[Value.StructValueFieldNumber];
                    var structMessage = JsonSerializer.Deserialize(ref reader, field.MessageType.ClrType, options);
                    field.Accessor.SetValue(message, structMessage);
                    break;
                }
            case JsonTokenType.StartArray:
                {
                    var field = fields[Value.ListValueFieldNumber];
                    var list = JsonSerializer.Deserialize(ref reader, field.MessageType.ClrType, options);
                    field.Accessor.SetValue(message, list);
                    break;
                }
            case JsonTokenType.Comment:
                break;
            case JsonTokenType.String:
                fields[Value.StringValueFieldNumber].Accessor.SetValue(message, reader.GetString()!);
                break;
            case JsonTokenType.Number:
                fields[Value.NumberValueFieldNumber].Accessor.SetValue(message, reader.GetDouble());
                break;
            case JsonTokenType.True:
                fields[Value.BoolValueFieldNumber].Accessor.SetValue(message, true);
                break;
            case JsonTokenType.False:
                fields[Value.BoolValueFieldNumber].Accessor.SetValue(message, false);
                break;
            case JsonTokenType.Null:
                fields[Value.NullValueFieldNumber].Accessor.SetValue(message, 0);
                break;
            default:
                throw new InvalidOperationException("Unexpected token type: " + reader.TokenType);
        }

        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        var specifiedField = value.Descriptor.Oneofs[0].Accessor.GetCaseFieldDescriptor(value);
        if (specifiedField == null)
        {
            throw new InvalidOperationException("Value message must contain a value for the oneof.");
        }

        object v = specifiedField.Accessor.GetValue(value);

        switch (specifiedField.FieldNumber)
        {
            case Value.BoolValueFieldNumber:
            case Value.StringValueFieldNumber:
            case Value.NumberValueFieldNumber:
            case Value.StructValueFieldNumber:
            case Value.ListValueFieldNumber:
                JsonSerializer.Serialize(writer, v, v.GetType(), options);
                break;
            case Value.NullValueFieldNumber:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidOperationException("Unexpected case in struct field: " + specifiedField.FieldNumber);
        }
    }
}

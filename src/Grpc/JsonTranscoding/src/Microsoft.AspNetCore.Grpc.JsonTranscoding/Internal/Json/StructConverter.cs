// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class StructConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public StructConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var message = new TMessage();
        JsonConverterHelper.PopulateMap(ref reader, options, message, message.Descriptor.Fields[Struct.FieldsFieldNumber]);

        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var fields = (IDictionary)value.Descriptor.Fields[Struct.FieldsFieldNumber].Accessor.GetValue(value);
        foreach (DictionaryEntry entry in fields)
        {
            var k = (string)entry.Key;
            var v = (IMessage?)entry.Value;
            if (string.IsNullOrEmpty(k) || v == null)
            {
                throw new InvalidOperationException("Struct fields cannot have an empty key or a null value.");
            }

            writer.WritePropertyName(k);
            JsonSerializer.Serialize(writer, v, v.GetType(), options);
        }

        writer.WriteEndObject();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class WrapperConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public WrapperConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var message = new TMessage();
        var valueDescriptor = message.Descriptor.Fields[JsonConverterHelper.WrapperValueFieldNumber];
        var t = JsonConverterHelper.GetFieldType(valueDescriptor);
        var value = JsonSerializer.Deserialize(ref reader, t, options);
        valueDescriptor.Accessor.SetValue(message, value);

        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        var valueDescriptor = value.Descriptor.Fields[JsonConverterHelper.WrapperValueFieldNumber];
        var innerValue = valueDescriptor.Accessor.GetValue(value);
        JsonSerializer.Serialize(writer, innerValue, innerValue.GetType(), options);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class ListValueConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public ListValueConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var message = new TMessage();
        JsonConverterHelper.PopulateList(ref reader, options, message, message.Descriptor.Fields[ListValue.ValuesFieldNumber]);

        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        var list = (IList)value.Descriptor.Fields[ListValue.ValuesFieldNumber].Accessor.GetValue(value);

        JsonSerializer.Serialize(writer, list, list.GetType(), options);
    }
}

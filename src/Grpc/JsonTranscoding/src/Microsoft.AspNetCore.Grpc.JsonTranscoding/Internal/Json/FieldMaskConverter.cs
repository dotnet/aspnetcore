// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class FieldMaskConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    public FieldMaskConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var message = new TMessage();

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new InvalidOperationException("Expected string value for FieldMask.");
        }
        // TODO: Do we *want* to remove empty entries? Probably okay to treat "" as "no paths", but "foo,,bar"?
        // Note: This logic replicates Google.Protobuf. Should follow their lead.
        var jsonPaths = reader.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var messagePaths = (IList)message.Descriptor.Fields[FieldMask.PathsFieldNumber].Accessor.GetValue(message);
        foreach (var path in jsonPaths)
        {
            messagePaths.Add(Legacy.ToSnakeCase(path));
        }

        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        // Note: This logic replicates Google.Protobuf. Should follow their lead.
        var paths = (IList<string>)value.Descriptor.Fields[FieldMask.PathsFieldNumber].Accessor.GetValue(value);
        var firstInvalid = paths.FirstOrDefault(p => !Legacy.IsPathValid(p));
        if (firstInvalid == null)
        {
            writer.WriteStringValue(Legacy.GetFieldMaskText(paths));
        }
        else
        {
            throw new InvalidOperationException($"Invalid field mask to be converted to JSON: {firstInvalid}.");
        }
    }
}

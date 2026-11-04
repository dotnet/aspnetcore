// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class NullValueConverter : JsonConverter<NullValue>
{
    public override bool HandleNull => true;

    public override NullValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.GetString() == "NULL_VALUE")
                {
                    return NullValue.NullValue;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid enum value: {reader.GetString()} for enum type: google.protobuf.NullValue");
                }
            case JsonTokenType.Number:
                return (NullValue)reader.GetInt32();
            case JsonTokenType.Null:
                return NullValue.NullValue;
            default:
                throw new InvalidOperationException($"Unexpected JSON token: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, NullValue value, JsonSerializerOptions options)
    {
        writer.WriteNullValue();
    }
}

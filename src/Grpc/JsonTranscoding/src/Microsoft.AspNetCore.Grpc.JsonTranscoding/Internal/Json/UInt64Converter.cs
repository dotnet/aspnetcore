// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class UInt64Converter : SettingsConverterBase<ulong>
{
    public UInt64Converter(JsonContext context) : base(context)
    {
    }

    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return ulong.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
        }

        return reader.GetUInt64();
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        if (!Context.Settings.WriteInt64sAsStrings)
        {
            writer.WriteNumberValue(value);
        }
        else
        {
            writer.WriteStringValue(value.ToString("d", CultureInfo.InvariantCulture));
        }
    }
}

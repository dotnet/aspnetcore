// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class ByteStringConverter : JsonConverter<ByteString>
{
    public override ByteString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TODO - handle base64 strings without padding
        return UnsafeByteOperations.UnsafeWrap(reader.GetBytesFromBase64());
    }

    public override void Write(Utf8JsonWriter writer, ByteString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToBase64());
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Identity;

internal sealed class BufferSourceJsonConverter : JsonConverter<BufferSource>
{
    public override BufferSource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null)
        {
            return null;
        }

        return BufferSource.FromBase64UrlString(value);
    }

    public override void Write(Utf8JsonWriter writer, BufferSource value, JsonSerializerOptions options)
    {
        var base64UrlString = value.AsBase64UrlString();
        writer.WriteStringValue(base64UrlString);
    }
}

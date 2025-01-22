// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Converters;

internal class OperationConverter<T> : JsonConverter<Operation<T>> where T : class
{
    public override bool CanConvert(Type typeToConvert)
    {
        var result = base.CanConvert(typeToConvert);
        return result;
    }

    public override Operation<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var op = root.GetProperty("op").GetString();
        var path = root.GetProperty("path").GetString();

        string from = null;
        if (root.TryGetProperty("from", out var fromProp))
        {
            from = fromProp.GetString();
        }

        object value = null;
        if (root.TryGetProperty("value", out var valueProp))
        {
            // Deserialize "value" into object using System.Text.Json – you might deserialize to T here if you prefer
            value = valueProp.Deserialize<object>(options);
        }

        return new Operation<T>
        {
            op = op,
            path = path,
            from = from,
            value = value
        };
    }

    public override void Write(Utf8JsonWriter writer, Operation<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("op", value.op);
        writer.WriteString("path", value.path);

        if (value.from != null)
        {
            writer.WriteString("from", value.from);
        }

        if (value.value != null)
        {
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.value, options);
        }

        writer.WriteEndObject();
    }
}

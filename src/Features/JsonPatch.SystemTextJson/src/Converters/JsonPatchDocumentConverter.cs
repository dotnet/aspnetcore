// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Converters;

internal class JsonPatchDocumentConverter : JsonConverter<JsonPatchDocument>
{
    private static JsonConverter<Operation> GetConverter(JsonSerializerOptions options) =>
            (JsonConverter<Operation>)options.GetConverter(typeof(Operation));

    public override JsonPatchDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert != typeof(JsonPatchDocument))
        {
            throw new ArgumentException(Resources.FormatParameterMustMatchType(nameof(typeToConvert), nameof(JsonPatchDocument)), nameof(typeToConvert));
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(Resources.InvalidJsonPatchDocument);
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        List<Operation> ops = [];

        try
        {
            JsonConverter<Operation> operationConverter = GetConverter(options);
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
            {
                var op = operationConverter.Read(ref reader, typeof(Operation), options);
                ops.Add(op);
            }

            return new JsonPatchDocument(ops, options);
        }
        catch (Exception ex)
        {
            throw new JsonException(Resources.InvalidJsonPatchDocument, ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, JsonPatchDocument value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonConverter<Operation> operationConverter = GetConverter(options);

        writer.WriteStartArray();
        foreach (var operation in value.Operations)
        {
            operationConverter.Write(writer, operation, options);
        }

        writer.WriteEndArray();
    }
}

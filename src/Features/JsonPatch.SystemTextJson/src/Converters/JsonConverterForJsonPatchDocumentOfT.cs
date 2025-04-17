// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Converters;

internal sealed class JsonConverterForJsonPatchDocumentOfT<T> : JsonConverter<JsonPatchDocument<T>>
    where T : class
{
    public override bool CanConvert(Type typeToConvert)
    {
        var result = base.CanConvert(typeToConvert);
        return result;
    }

    public override JsonPatchDocument<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Parse root object
        try
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var operationsElement = document.RootElement;
            if (operationsElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Expected 'Operations' array property in JsonPatchDocument.");
            }

            // Clone options with Operation<T> converter
            var effectiveOptions = CloneWithOperationConverter(options);

            // Deserialize the operations array
            var operations = JsonSerializer.Deserialize<List<Operation<T>>>(operationsElement.GetRawText(), effectiveOptions);

            return new JsonPatchDocument<T>(operations, options);

        }
        catch (Exception ex)
        {
            throw new JsonException(Resources.InvalidJsonPatchDocument, ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, JsonPatchDocument<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Operations, CloneWithOperationConverter(options));
    }

    private static JsonSerializerOptions CloneWithOperationConverter(JsonSerializerOptions baseOptions)
    {
        var options = baseOptions;

        var converterRegistered = IsOperationConverterRegistered(options);
        if (!converterRegistered)
        {
            options = new JsonSerializerOptions(baseOptions);
            options.Converters.Add(new OperationConverterFactory());
        }

        return options;
    }

    private static bool IsOperationConverterRegistered(JsonSerializerOptions options)
    {
        for (var i = 0; i < options.Converters.Count; i++)
        {
            var converter = options.Converters[i];
            if (converter is OperationConverterFactory || converter.GetType().IsGenericType && converter.GetType().GetGenericTypeDefinition() == typeof(OperationConverter<>))
            {
                return true;
            }
        }

        return false;
    }
}

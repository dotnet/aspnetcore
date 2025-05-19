// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A custom JsonConverter for HttpValidationProblemDetails that respects the JsonSerializerOptions.DictionaryKeyPolicy
/// when serializing validation error property names.
/// </summary>
internal sealed class HttpValidationProblemDetailsJsonConverter : JsonConverter<HttpValidationProblemDetails>
{
    public override HttpValidationProblemDetails? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Use the default deserialization
        return JsonSerializer.Deserialize<HttpValidationProblemDetails>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write all the standard ProblemDetails properties
        if (value.Title != null)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Title") ?? "Title");
            writer.WriteStringValue(value.Title);
        }

        if (value.Type != null)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Type") ?? "Type");
            writer.WriteStringValue(value.Type);
        }

        if (value.Detail != null)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Detail") ?? "Detail");
            writer.WriteStringValue(value.Detail);
        }

        if (value.Status != null)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Status") ?? "Status");
            writer.WriteNumberValue(value.Status.Value);
        }

        if (value.Instance != null)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Instance") ?? "Instance");
            writer.WriteStringValue(value.Instance);
        }

        // Write the "errors" property with transformed keys based on DictionaryKeyPolicy
        if (value.Errors != null && value.Errors.Count > 0)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Errors") ?? "Errors");
            writer.WriteStartObject();

            foreach (var error in value.Errors)
            {
                // Apply DictionaryKeyPolicy to the error key if available
                var transformedKey = options.DictionaryKeyPolicy?.ConvertName(error.Key) ?? error.Key;
                writer.WritePropertyName(transformedKey);

                // Write the error messages array
                JsonSerializer.Serialize(writer, error.Value, options);
            }

            writer.WriteEndObject();
        }

        // Handle additional extension members
        foreach (var extension in value.Extensions)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(extension.Key) ?? extension.Key);
            JsonSerializer.Serialize(writer, extension.Value, options);
        }

        writer.WriteEndObject();
    }
}
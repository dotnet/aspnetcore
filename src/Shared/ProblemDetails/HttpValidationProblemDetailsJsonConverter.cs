// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class HttpValidationProblemDetailsJsonConverter : JsonConverter<HttpValidationProblemDetails>
{
    private static readonly JsonEncodedText Errors = JsonEncodedText.Encode("errors");

    public override HttpValidationProblemDetails Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var problemDetails = new HttpValidationProblemDetails();
        return ReadProblemDetails(ref reader, options, problemDetails);
    }

    public static HttpValidationProblemDetails ReadProblemDetails(ref Utf8JsonReader reader, JsonSerializerOptions options, HttpValidationProblemDetails problemDetails)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Unexpected end when reading JSON.");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals(Errors.EncodedUtf8Bytes))
            {
                ReadErrors(ref reader, problemDetails.Errors);
            }
            else
            {
                ProblemDetailsJsonConverter.ReadValue(ref reader, problemDetails, options);
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException("Unexpected end when reading JSON.");
        }

        return problemDetails;

        static void ReadErrors(ref Utf8JsonReader reader, IDictionary<string, string[]> errors)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end when reading JSON.");
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        var name = reader.GetString()!;

                        if (!reader.Read())
                        {
                            throw new JsonException("Unexpected end when reading JSON.");
                        }

                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            errors[name] = null!;
                        }
                        else
                        {
                            var values = new List<string>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                values.Add(reader.GetString()!);
                            }
                            errors[name] = values.ToArray();
                        }
                    }
                    break;
                case JsonTokenType.Null:
                    return;
                default:
                    throw new JsonException($"Unexpected token when reading errors: {reader.TokenType}");
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        WriteProblemDetails(writer, value, options);
    }

    public static void WriteProblemDetails(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        ProblemDetailsJsonConverter.WriteProblemDetails(writer, value, options);

        writer.WritePropertyName(Errors);
        WriteErrors(writer, value, options);

        writer.WriteEndObject();

        static void WriteErrors(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value.Errors)
            {
                var name = kvp.Key;
                var errors = kvp.Value;

                writer.WritePropertyName(options.DictionaryKeyPolicy?.ConvertName(name) ?? name);
                if (errors is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (var error in errors)
                    {
                        writer.WriteStringValue(error);
                    }
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();
        }
    }
}

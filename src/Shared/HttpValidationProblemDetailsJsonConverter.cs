// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http;

internal sealed class HttpValidationProblemDetailsJsonConverter : JsonConverter<HttpValidationProblemDetails>
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
            throw new JsonException("Unexcepted end when reading JSON.");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals(Errors.EncodedUtf8Bytes))
            {
                var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(ref reader, options);
                if (errors is not null)
                {
                    foreach (var item in errors)
                    {
                        problemDetails.Errors[item.Key] = item.Value;
                    }
                }
            }
            else
            {
                ProblemDetailsJsonConverter.ReadValue(ref reader, problemDetails, options);
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException("Unexcepted end when reading JSON.");
        }

        return problemDetails;
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
        JsonSerializer.Serialize(writer, value.Errors, options);

        writer.WriteEndObject();
    }
}

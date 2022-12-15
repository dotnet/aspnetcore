// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class HttpValidationProblemDetailsJsonConverter : JsonConverter<HttpValidationProblemDetails>
{
    private static readonly JsonEncodedText Errors = JsonEncodedText.Encode("errors");

    [RequiresUnreferencedCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2046:'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3051:'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "<Pending>")]
    public override HttpValidationProblemDetails Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var problemDetails = new HttpValidationProblemDetails();
        return ReadProblemDetails(ref reader, options, problemDetails);
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
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
                var context = new ErrorsJsonContext(options);
                var errors = JsonSerializer.Deserialize(ref reader, context.DictionaryStringStringArray);
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
            throw new JsonException("Unexpected end when reading JSON.");
        }

        return problemDetails;
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2046:'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3051:'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "<Pending>")]
    public override void Write(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        WriteProblemDetails(writer, value, options);
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization and deserialization of ProblemDetails.Extensions might require types that cannot be statically analyzed.")]
    public static void WriteProblemDetails(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        ProblemDetailsJsonConverter.WriteProblemDetails(writer, value, options);

        writer.WritePropertyName(Errors);

        var context = new ErrorsJsonContext(options);
        JsonSerializer.Serialize(writer, value.Errors, context.IDictionaryStringStringArray);

        writer.WriteEndObject();
    }

    [JsonSerializable(typeof(IDictionary<string, string[]>))]
    [JsonSerializable(typeof(Dictionary<string, string[]>))]
    private sealed partial class ErrorsJsonContext : JsonSerializerContext
    { }
}

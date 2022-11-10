// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http;

internal sealed class HttpValidationProblemDetailsJsonConverter : JsonConverter<HttpValidationProblemDetails>
{
    private static readonly JsonEncodedText Errors = JsonEncodedText.Encode("errors");

    [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "Trimmer does not allow annotating overriden methods with annotations different from the ones in base type.")]
    public override HttpValidationProblemDetails Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var problemDetails = new HttpValidationProblemDetails();
        return ReadProblemDetails(ref reader, options, problemDetails);
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization ProblemDetails.Extensions might require types that cannot be statically analyzed. ")]
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
                var errors = DeserializeErrors(ref reader, options);
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

        [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "We ensure Dictionary<string, string[]> is preserved.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(Dictionary<string, string[]>))]
        static Dictionary<string, string[]>? DeserializeErrors(ref Utf8JsonReader reader, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<Dictionary<string, string[]>>(ref reader, options);
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "Trimmer does not allow annotating overriden methods with annotations different from the ones in base type.")]
    public override void Write(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        WriteProblemDetails(writer, value, options);
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization ProblemDetails.Extensions might require types that cannot be statically analyzed. ")]
    public static void WriteProblemDetails(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        ProblemDetailsJsonConverter.WriteProblemDetails(writer, value, options);

        writer.WritePropertyName(Errors);
        SerializeErrors(writer, value.Errors, options);

        writer.WriteEndObject();

        [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "We ensure IDictionary<string, string[]> is preserved.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(IDictionary<string, string[]>))]
        static void SerializeErrors(Utf8JsonWriter writer, IDictionary<string, string[]> errors, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, errors, options);
    }
}

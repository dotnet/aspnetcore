// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// A RFC 7807 compliant <see cref="JsonConverter"/> for <see cref="ValidationProblemDetails"/>.
/// </summary>
public sealed class ValidationProblemDetailsConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ValidationProblemDetails);
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var annotatedProblemDetails = serializer.Deserialize<AnnotatedValidationProblemDetails>(reader);
        if (annotatedProblemDetails == null)
        {
            return null;
        }

        var problemDetails = (ValidationProblemDetails?)existingValue ?? new ValidationProblemDetails();
        annotatedProblemDetails.CopyTo(problemDetails);

        return problemDetails;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var problemDetails = (ValidationProblemDetails)value;
        var annotatedProblemDetails = new AnnotatedValidationProblemDetails(problemDetails);

        serializer.Serialize(writer, annotatedProblemDetails);
    }

    private sealed class AnnotatedValidationProblemDetails : AnnotatedProblemDetails
    {
        /// <remarks>
        /// Required for JSON.NET deserialization.
        /// </remarks>
        public AnnotatedValidationProblemDetails() { }

        public AnnotatedValidationProblemDetails(ValidationProblemDetails problemDetails)
            : base(problemDetails)
        {
            foreach (var kvp in problemDetails.Errors)
            {
                Errors[kvp.Key] = kvp.Value;
            }
        }

        [JsonProperty(PropertyName = "errors")]
        public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public void CopyTo(ValidationProblemDetails problemDetails)
        {
            base.CopyTo(problemDetails);

            foreach (var kvp in Errors)
            {
                problemDetails.Errors[kvp.Key] = kvp.Value;
            }
        }
    }
}

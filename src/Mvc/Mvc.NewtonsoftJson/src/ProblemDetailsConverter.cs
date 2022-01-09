// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// A RFC 7807 compliant <see cref="JsonConverter"/> for <see cref="ProblemDetails"/>.
/// </summary>
public sealed class ProblemDetailsConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ProblemDetails);
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var annotatedProblemDetails = serializer.Deserialize<AnnotatedProblemDetails>(reader);
        if (annotatedProblemDetails == null)
        {
            return null;
        }

        var problemDetails = (ProblemDetails?)existingValue ?? new ProblemDetails();
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

        var problemDetails = (ProblemDetails)value;
        var annotatedProblemDetails = new AnnotatedProblemDetails(problemDetails);

        serializer.Serialize(writer, annotatedProblemDetails);
    }
}

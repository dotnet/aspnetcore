// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class DefaultProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly ProblemDetailsOptions _options;

    public DefaultProblemDetailsWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
    }

    public bool CanWrite(HttpContext context, EndpointMetadataCollection? metadata, bool isRouting)
        => (isRouting || context.Response.StatusCode >= 500);

    public Task WriteAsync(
        HttpContext context,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        ProblemDetailsDefaults.Apply(problemDetails, context.Response.StatusCode);
        _options.ConfigureDetails?.Invoke(context, problemDetails);

        return context.Response.WriteAsJsonAsync(problemDetails, typeof(ProblemDetails), ProblemDetailsJsonContext.Default, contentType: "application/problem+json");
    }

    [JsonSerializable(typeof(ProblemDetails))]
    internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
    { }
}

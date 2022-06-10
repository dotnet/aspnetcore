// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly ProblemDetailsOptions _options;

    public DefaultProblemDetailsWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
    }

    public bool CanWrite(HttpContext context, EndpointMetadataCollection? metadata, bool isRouting)
    {
        if (isRouting || context.Response.StatusCode >= 500)
        {
            return true;
        }

        var problemDetailsMetadata = metadata?.GetMetadata<ProblemDetailsResponseMetadata>();
        return problemDetailsMetadata != null;

        //var headers = context.Request.GetTypedHeaders();
        //var acceptHeader = headers.Accept;
        //if (acceptHeader != null &&
        //    !acceptHeader.Any(h => _problemMediaType.IsSubsetOf(h)))
        //{
        //    return false;
        //}
    }

    public Task WriteAsync(
        HttpContext context,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null,
        Action<HttpContext, ProblemDetails>? configureDetails = null)
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
        configureDetails?.Invoke(context, problemDetails);

        return context.Response.WriteAsJsonAsync(problemDetails, typeof(ProblemDetails), ProblemDetailsJsonContext.Default, contentType: "application/problem+json");
    }
}


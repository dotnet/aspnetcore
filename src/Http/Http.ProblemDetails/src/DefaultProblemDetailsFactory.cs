// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultHttpProblemDetailsFactory : IHttpProblemDetailsFactory
{
    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        ProblemDetailsOptions options,
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
            Instance = instance,
        };

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions.Add(extension);
            }
        }

        ProblemDetailsDefaults.Apply(httpContext, problemDetails, statusCode, options.ProblemDetailsErrorMapping);

        return problemDetails;
    }

    public async Task WriteAsync(
        HttpContext context,
        ProblemDetails problemDetails)
    {
        //TODO: JsonOptions??? CancellationToken??
        try
        {
            context.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, typeof(ProblemDetails), cancellationToken: context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
    }
}

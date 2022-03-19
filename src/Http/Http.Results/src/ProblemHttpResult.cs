// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="IResult"/> that on execution will write Problem Details
/// HTTP API responses based on https://tools.ietf.org/html/rfc7807
/// </summary>
public sealed class ProblemHttpResult : IResult
{
    /// <summary>
    /// Creates a new <see cref="PhysicalFileHttpResult"/> instance with
    /// the provided <paramref name="problemDetails"/>.
    /// </summary>
    /// <param name="problemDetails">The <see cref="ProblemDetails"/> instance to format in the entity body.</param>
    internal ProblemHttpResult(ProblemDetails problemDetails)
    {
        ProblemDetails = problemDetails;
        HttpResultsHelper.ApplyProblemDetailsDefaults(ProblemDetails, statusCode: null);
    }

    /// <summary>
    /// Gets the <see cref="ProblemDetails"/> instance.
    /// </summary>
    public ProblemDetails ProblemDetails { get; }

    /// <summary>
    /// Gets or sets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string ContentType => "application/problem+json";

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode => ProblemDetails.Status;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(ProblemHttpResult));

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                value: ProblemDetails,
                StatusCode,
                ContentType);
    }
}

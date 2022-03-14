// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="IResult"/> that on execution will write Problem Details
/// HTTP API responses based on https://tools.ietf.org/html/rfc7807
/// </summary>
public sealed class ProblemHttpResult : IResult, IProblemHttpResult
{
    internal ProblemHttpResult(ProblemDetails problemDetails)
    {
        ProblemDetails = problemDetails;
    }

    /// <summary>
    /// Gets the <see cref="ProblemDetails"/> instance.
    /// </summary>
    public ProblemDetails ProblemDetails { get; }

    /// <summary>
    /// Gets or sets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string ContentType => "application/problem+json";

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, ProblemDetails, ContentType);
}

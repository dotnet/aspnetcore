// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write Problem Details
/// HTTP API responses based on <see href="https://tools.ietf.org/html/rfc7807"/>
/// </summary>
public sealed class ProblemHttpResult : IResult, IStatusCodeHttpResult, IContentTypeHttpResult, IValueHttpResult, IValueHttpResult<ProblemDetails>
{
    /// <summary>
    /// Creates a new <see cref="ProblemHttpResult"/> instance with
    /// the provided <paramref name="problemDetails"/>.
    /// </summary>
    /// <param name="problemDetails">The <see cref="ProblemDetails"/> instance to format in the entity body.</param>
    internal ProblemHttpResult(ProblemDetails problemDetails)
    {
        ProblemDetails = problemDetails;
        ProblemDetailsDefaults.Apply(ProblemDetails, statusCode: null);
    }

    /// <summary>
    /// Gets the <see cref="ProblemDetails"/> instance.
    /// </summary>
    public ProblemDetails ProblemDetails { get; }

    object? IValueHttpResult.Value => ProblemDetails;

    ProblemDetails? IValueHttpResult<ProblemDetails>.Value => ProblemDetails;

    /// <summary>
    /// Gets the value for the <c>Content-Type</c> header: <c>application/problem+json</c>
    /// </summary>
    public string ContentType => ContentTypeConstants.ProblemDetailsContentType;

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => ProblemDetails.Status!.Value;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(ProblemHttpResult));
        var problemDetailsService = httpContext.RequestServices.GetService<IProblemDetailsService>();

        if (StatusCode is { } code)
        {
            HttpResultsHelper.Log.WritingResultAsStatusCode(logger, code);
            httpContext.Response.StatusCode = code;
        }

        if (problemDetailsService is null || !await problemDetailsService.TryWriteAsync(new() { HttpContext = httpContext, ProblemDetails = ProblemDetails }))
        {
            await HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                value: ProblemDetails,
                ContentType);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write Problem Details
/// HTTP API responses based on https://tools.ietf.org/html/rfc7807
/// </summary>
public sealed class ValidationProblem : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IContentTypeHttpResult, IValueHttpResult, IValueHttpResult<HttpValidationProblemDetails>
{
    internal ValidationProblem(HttpValidationProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        if (problemDetails is { Status: not null and not StatusCodes.Status400BadRequest })
        {
            throw new ArgumentException($"{nameof(ValidationProblem)} only supports a 400 Bad Request response status code.", nameof(problemDetails));
        }

        ProblemDetails = problemDetails;
        ProblemDetailsDefaults.Apply(ProblemDetails, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Gets the <see cref="HttpValidationProblemDetails"/> instance.
    /// </summary>
    public HttpValidationProblemDetails ProblemDetails { get; }

    object? IValueHttpResult.Value => ProblemDetails;

    HttpValidationProblemDetails? IValueHttpResult<HttpValidationProblemDetails>.Value => ProblemDetails;

    /// <summary>
    /// Gets the value for the <c>Content-Type</c> header: <c>application/problem+json</c>.
    /// </summary>
    public string ContentType => ContentTypeConstants.ProblemDetailsContentType;

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status400BadRequest"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status400BadRequest;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(ValidationProblem));
        var problemDetailsService = httpContext.RequestServices.GetService<IProblemDetailsService>();

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        if (problemDetailsService is null || !await problemDetailsService.TryWriteAsync(new() { HttpContext = httpContext, ProblemDetails = ProblemDetails }))
        {
            await HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                value: ProblemDetails,
                ContentType);
        }
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(ProducesResponseTypeMetadata.CreateUnvalidated(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, ContentTypeConstants.ProblemDetailsContentTypes));
    }
}

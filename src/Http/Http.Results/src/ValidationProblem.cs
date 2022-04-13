// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write Problem Details
/// HTTP API responses based on https://tools.ietf.org/html/rfc7807
/// </summary>
public sealed class ValidationProblem : IResult, IEndpointMetadataProvider
{
    /// <summary>
    /// Creates a new <see cref="ValidationProblem"/> instance with
    /// the provided <paramref name="problemDetails"/>.
    /// </summary>
    /// <param name="problemDetails">The <see cref="HttpValidationProblemDetails"/> instance to format in the entity body.</param>
    internal ValidationProblem(HttpValidationProblemDetails problemDetails)
    {
        ProblemDetails = problemDetails;
        HttpResultsHelper.ApplyProblemDetailsDefaults(ProblemDetails, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Gets the <see cref="HttpValidationProblemDetails"/> instance.
    /// </summary>
    public HttpValidationProblemDetails ProblemDetails { get; }

    /// <summary>
    /// Gets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string ContentType => "application/problem+json";

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status400BadRequest;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(ValidationProblem));

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                value: ProblemDetails,
                ContentType);
    }

    /// <inheritdoc/>
    public static void PopulateMetadata(EndpointMetadataContext context)
    {
        context.EndpointMetadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"));
    }
}

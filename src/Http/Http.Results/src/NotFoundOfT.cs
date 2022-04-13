// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Not Found (404) status code.
/// </summary>
/// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
public sealed class NotFound<TValue> : IResult, IEndpointMetadataProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFound"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    internal NotFound(TValue? value)
    {
        Value = value;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public TValue? Value { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status404NotFound"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status404NotFound;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.NotFoundObjectResult");

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger: logger,
                Value);
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(EndpointMetadataContext context)
    {
        context.EndpointMetadata.Add(new ProducesResponseTypeMetadata(typeof(TValue), StatusCodes.Status404NotFound, "application/json"));
    }
}

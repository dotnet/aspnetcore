// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Bad Request (400) status code.
/// </summary>
public sealed class BadRequestObjectHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestObjectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="error">The error content to format in the entity body.</param>
    internal BadRequestObjectHttpResult(object? error)
    {
        Value = error;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status400BadRequest;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.BadRequestObjectResult");

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger: logger,
                Value,
                StatusCode);
    }
}

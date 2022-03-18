// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response.
/// </summary>
internal sealed class ObjectHttpResult : IResult
{
    /// <summary>
    /// Creates a new <see cref="ObjectHttpResult"/> instance
    /// with the provided <paramref name="value"/>.
    /// </summary>
    internal ObjectHttpResult(object? value)
        : this(value, null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ObjectHttpResult"/> instance with the provided
    /// <paramref name="value"/>, <paramref name="statusCode"/>.
    /// </summary>
    internal ObjectHttpResult(object? value, int? statusCode)
        : this(value, statusCode, contentType: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ObjectHttpResult"/> instance with the provided
    /// <paramref name="value"/>, <paramref name="statusCode"/> and <paramref name="contentType"/>.
    /// </summary>
    internal ObjectHttpResult(object? value, int? statusCode, string? contentType)
    {
        Value = value;

        if (value is ProblemDetails problemDetails)
        {
            HttpResultsHelper.ApplyProblemDetailsDefaults(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }

        StatusCode = statusCode;
        ContentType = contentType;
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// Gets or sets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; internal init; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.ObjectResult");

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger: logger,
                Value,
                StatusCode,
                ContentType);
    }
}

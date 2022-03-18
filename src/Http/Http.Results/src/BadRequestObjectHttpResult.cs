// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

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
    public BadRequestObjectHttpResult(object? error)
    {
        Value = error;
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
        => HttpResultsWriter.WriteResultAsJsonAsync(httpContext, Value, StatusCode);
}

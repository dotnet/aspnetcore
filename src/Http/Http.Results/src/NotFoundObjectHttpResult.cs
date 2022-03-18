// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Not Found (404) status code.
/// </summary>
public sealed class NotFoundObjectHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundObjectHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    internal NotFoundObjectHttpResult(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status404NotFound;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJsonAsync(httpContext, Value, StatusCode);
}

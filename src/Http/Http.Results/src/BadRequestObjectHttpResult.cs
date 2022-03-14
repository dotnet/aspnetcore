// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Bad Request (400) status code.
/// </summary>
public sealed class BadRequestObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    internal BadRequestObjectHttpResult(object? error)
    {
        Value = error;
    }

    /// <inheritdoc/>
    public object? Value { get; internal init; }

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status400BadRequest;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, objectHttpResult: this);
}

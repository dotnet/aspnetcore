// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Not Found (404) status code.
/// </summary>
public sealed class NotFoundObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    internal NotFoundObjectHttpResult(object? value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    public object? Value { get; internal init; }

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status404NotFound;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, objectHttpResult: this);
}

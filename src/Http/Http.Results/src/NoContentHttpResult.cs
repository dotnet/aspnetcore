// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// produce an HTTP response with the No Content (204) status code.
/// </summary>
public class NoContentHttpResult : IResult, IStatusCodeHttpResult
{
    internal NoContentHttpResult()
    {
    }

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status204NoContent;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        HttpResultsWriter.WriteResultAsStatusCode(httpContext, statusCodeHttpResult: this);
        return Task.CompletedTask;
    }
}

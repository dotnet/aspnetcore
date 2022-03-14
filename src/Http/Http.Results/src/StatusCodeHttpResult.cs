// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// produce an HTTP response with the given response status code.
/// </summary>
internal sealed partial class StatusCodeHttpResult : IResult, IStatusCodeHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeHttpResult"/> class
    /// with the given <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    internal StatusCodeHttpResult(int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <inheritdoc/>
    public int? StatusCode { get; }

    /// <summary>
    /// Sets the status code on the HTTP response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        HttpResultsWriter.WriteResultAsStatusCode(httpContext, statusCodeHttpResult: this);
        return Task.CompletedTask;
    }
}

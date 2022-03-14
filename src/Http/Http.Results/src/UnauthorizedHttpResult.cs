// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed class UnauthorizedHttpResult : IResult, IStatusCodeHttpResult
{
    private readonly int _statuCode = StatusCodes.Status401Unauthorized;

    internal UnauthorizedHttpResult()
    {
    }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode => _statuCode;

    /// <summary>
    /// Sets the status code on the HTTP response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        HttpResultsWriter.SetHttpStatusCode(httpContext, _statuCode);
        return Task.CompletedTask;
    }

}

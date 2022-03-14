// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public class NoContentHttpResult : IResult, IStatusCodeHttpResult
{
    private readonly int _statuCode = StatusCodes.Status204NoContent;

    internal NoContentHttpResult()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode => _statuCode;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        HttpResultsWriter.WriteResultAsStatusCode(httpContext, _statuCode);
        return Task.CompletedTask;
    }
}

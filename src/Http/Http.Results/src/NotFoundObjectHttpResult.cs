// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed class NotFoundObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    internal NotFoundObjectHttpResult(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode => StatusCodes.Status404NotFound;

    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, statusCode: StatusCode);
}

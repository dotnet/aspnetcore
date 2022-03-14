// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed class UnprocessableEntityObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    internal UnprocessableEntityObjectHttpResult(object? error)
    {
        Value = error;
    }

    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode => StatusCodes.Status422UnprocessableEntity;

    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, statusCode: StatusCode);
}

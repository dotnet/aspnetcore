// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

/// <summary>
/// 
/// </summary>
public sealed class OkObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    internal OkObjectHttpResult(object? value)
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
    public int? StatusCode => StatusCodes.Status200OK;

    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, statusCode: StatusCode);
}

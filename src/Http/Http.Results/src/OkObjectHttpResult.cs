// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Ok (200) status code.
/// </summary>
public sealed class OkObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    internal OkObjectHttpResult(object? value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    public object? Value { get; internal init; }

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, objectHttpResult: this);
}

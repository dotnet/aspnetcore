// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Ok (200) status code.
/// </summary>
public sealed class OkObjectHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OkObjectHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    public OkObjectHttpResult(object? value)
    {
        Value = value;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsHelper.WriteResultAsJsonAsync(httpContext, Value, StatusCode);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Unprocessable Entity (422) status code.
/// </summary>
public sealed class UnprocessableEntityObjectHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictObjectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="error">The error content to format in the entity body.</param>
    public UnprocessableEntityObjectHttpResult(object? error)
    {
        Value = error;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <inheritdoc />
    public object? Value { get; internal init; }

    /// <inheritdoc />>
    public int StatusCode => StatusCodes.Status422UnprocessableEntity;

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsHelper.WriteResultAsJsonAsync(httpContext, Value, StatusCode);
}

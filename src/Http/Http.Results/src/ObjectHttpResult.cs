// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response.
/// </summary>
internal sealed class ObjectHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    /// <summary>
    /// Creates a new <see cref="ObjectHttpResult"/> instance
    /// with the provided <paramref name="value"/>.
    /// </summary>
    internal ObjectHttpResult(object? value)
        : this(value, null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ObjectHttpResult"/> instance with the provided
    /// <paramref name="value"/> and <paramref name="statusCode"/>.
    /// </summary>
    internal ObjectHttpResult(object? value, int? statusCode)
    {
        Value = value;

        if (value is ProblemDetails problemDetails)
        {
            HttpResultsWriter.ApplyProblemDetailsDefaults(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }

        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; internal init; }

    /// <summary>
    /// Gets or sets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; internal init; }

    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode { get; internal init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, ContentType, StatusCode);
}

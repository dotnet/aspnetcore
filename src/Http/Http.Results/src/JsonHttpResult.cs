// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An action result which formats the given object as JSON.
/// </summary>
public sealed class JsonHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    public JsonHttpResult(object? value)
        : this(value, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    public JsonHttpResult(object? value, JsonSerializerOptions? jsonSerializerOptions)
    {
        Value = value;
        JsonSerializerOptions = jsonSerializerOptions;

        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets or sets the serializer settings.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }

    /// <summary>
    /// Gets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.JsonResult");

        return HttpResultsHelper.WriteResultAsJsonAsync(
            httpContext,
            logger,
            Value,
            StatusCode,
            ContentType,
            JsonSerializerOptions);
    }
}

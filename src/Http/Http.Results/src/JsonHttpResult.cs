// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

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
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    public JsonHttpResult(object? value, JsonSerializerOptions? jsonSerializerOptions)
        : this(value, statusCode: null, contentType: null, jsonSerializerOptions: jsonSerializerOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    public JsonHttpResult(object? value, int? statusCode, JsonSerializerOptions? jsonSerializerOptions)
        : this(value, statusCode: statusCode, contentType: null, jsonSerializerOptions: jsonSerializerOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="contentType">The value for the <c>Content-Type</c> header</param>
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    public JsonHttpResult(object? value, string? contentType, JsonSerializerOptions? jsonSerializerOptions)
        : this(value, statusCode: null, contentType: contentType, jsonSerializerOptions: jsonSerializerOptions)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonHttpResult"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    /// <param name="contentType">The value for the <c>Content-Type</c> header</param>
    public JsonHttpResult(object? value, int? statusCode, string? contentType, JsonSerializerOptions? jsonSerializerOptions)
    {
        Value = value;
        StatusCode = statusCode;
        JsonSerializerOptions = jsonSerializerOptions;
        ContentType = contentType;
    }

    /// <summary>
    /// Gets or sets the serializer settings.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; internal init; }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; internal set; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJsonAsync(
            httpContext,
            Value,
            StatusCode,
            ContentType,
            JsonSerializerOptions);
}

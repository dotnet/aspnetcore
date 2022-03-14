// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An action result which formats the given object as JSON.
/// </summary>
public sealed class JsonHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult
{
    public JsonHttpResult(object? value, JsonSerializerOptions? jsonSerializerOptions)
        : this(value, statusCode: null, jsonSerializerOptions: jsonSerializerOptions)
    {
    }

    public JsonHttpResult(object? value, int? statusCode, JsonSerializerOptions? jsonSerializerOptions)
    {
        Value = value;
        StatusCode = statusCode;
        JsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// Gets or sets the serializer settings.
    /// <para>
    /// When using <c>System.Text.Json</c>, this should be an instance of <see cref="JsonSerializerOptions" />
    /// </para>
    /// <para>
    /// When using <c>Newtonsoft.Json</c>, this should be an instance of <c>JsonSerializerSettings</c>.
    /// </para>
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }

    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets or sets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; internal set; }

    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode { get; }

    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, ContentType, StatusCode, JsonSerializerOptions);
}

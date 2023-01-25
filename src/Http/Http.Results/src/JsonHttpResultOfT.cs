// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An action result which formats the given object as JSON.
/// </summary>
public sealed partial class JsonHttpResult<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>, IContentTypeHttpResult
{
    private readonly JsonTypeInfo? _jsonTypeInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="Json"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="contentType">The value for the <c>Content-Type</c> header</param>
    [RequiresDynamicCode(JsonHttpResultTrimmerWarning.SerializationRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(JsonHttpResultTrimmerWarning.SerializationUnreferencedCodeMessage)]
    internal JsonHttpResult(TValue? value, JsonSerializerOptions? jsonSerializerOptions, int? statusCode = null, string? contentType = null)
    {
        Value = value;
        ContentType = contentType;

        if (value is ProblemDetails problemDetails)
        {
            ProblemDetailsDefaults.Apply(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }
        StatusCode = statusCode;

        if (jsonSerializerOptions is not null && !jsonSerializerOptions.IsReadOnly)
        {
            jsonSerializerOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
        }

        JsonSerializerOptions = jsonSerializerOptions;
    }

    internal JsonHttpResult(TValue? value, JsonSerializerContext jsonSerializerContext, int? statusCode = null, string? contentType = null)
        : this(value, jsonSerializerContext.GetTypeInfo(typeof(TValue))!, statusCode, contentType)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Json"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="contentType">The value for the <c>Content-Type</c> header</param>
    internal JsonHttpResult(TValue? value, JsonTypeInfo jsonTypeInfo, int? statusCode = null, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        Value = value;
        ContentType = contentType;

        if (value is ProblemDetails problemDetails)
        {
            ProblemDetailsDefaults.Apply(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }

        StatusCode = statusCode;

        _jsonTypeInfo = jsonTypeInfo;
        JsonSerializerOptions = jsonTypeInfo.Options;
    }

    /// <summary>
    /// Gets or sets the serializer settings.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public TValue? Value { get; }

    object? IValueHttpResult.Value => Value;

    /// <summary>
    /// Gets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.JsonResult");

        if (StatusCode is { } statusCode)
        {
            HttpResultsHelper.Log.WritingResultAsStatusCode(logger, statusCode);
            httpContext.Response.StatusCode = statusCode;
        }

        if (_jsonTypeInfo is not null)
        {
            return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                Value,
                _jsonTypeInfo,
                ContentType);
        }

        return HttpResultsHelper.WriteResultAsJsonAsync(
            httpContext,
            logger,
            Value,
            ContentType,
            JsonSerializerOptions);
    }
}

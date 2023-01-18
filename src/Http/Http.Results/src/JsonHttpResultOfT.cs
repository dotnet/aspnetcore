// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
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
    private readonly JsonTypeInfo<TValue>? _jsonTypeInfo;
    private int _statusCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="Json"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="jsonSerializerOptions">The serializer settings.</param>
    internal JsonHttpResult(TValue? value, JsonSerializerOptions? jsonSerializerOptions)
    {
        Value = value;

        if (jsonSerializerOptions is not null && !jsonSerializerOptions.IsReadOnly) // Switch
        {
            jsonSerializerOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
        }

        JsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Json"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="jsonTypeInfo">The serializer type info.</param>
    internal JsonHttpResult(TValue? value, JsonTypeInfo<TValue> jsonTypeInfo)
    {
        Value = value;
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
    public string? ContentType { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode
    {
        get => _statusCode;
        internal init
        {
            var statusCode = value;
            if (Value is ProblemDetails problemDetails)
            {
                ProblemDetailsDefaults.Apply(problemDetails, statusCode);
                statusCode ??= problemDetails.Status;
            }

            _statusCode = statusCode!.Value;
        }
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.JsonResult");

        if (StatusCode is { } statusCode)
        {
            HttpResultsWriter.Log.WritingResultAsStatusCode(logger, statusCode);
            httpContext.Response.StatusCode = statusCode;
        }

        if (_jsonTypeInfo is null)
        {
            return HttpResultsWriter.WriteResultAsJsonAsync(
                httpContext,
                logger,
                Value,
                ContentType,
                JsonSerializerOptions);
        }

        return HttpResultsWriter.WriteResultAsJsonAsync(
            httpContext,
            logger,
            Value,
            _jsonTypeInfo,
            ContentType);
    }
}

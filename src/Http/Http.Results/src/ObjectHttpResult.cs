// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response.
/// </summary>
public partial class ObjectHttpResult : StatusCodeHttpResult
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

        if (Value is ProblemDetails problemDetails)
        {
            statusCode = ApplyProblemDetailsDefaults(problemDetails, statusCode);
        }

        if (statusCode is { } status)
        {
            StatusCode = status;
        }
    }

    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Gets or sets the value for the <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; init; }

    internal override Task WriteContentAsync(HttpContext httpContext)
    {
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(GetType());
        Log.ObjectResultExecuting(logger, Value, StatusCode);

        ConfigureResponseHeaders(httpContext);

        if (Value is null)
        {
            return Task.CompletedTask;
        }

        OnFormatting(httpContext);
        return WriteHttpResultAsync(httpContext);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    protected internal virtual void OnFormatting(HttpContext httpContext)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    protected internal virtual void ConfigureResponseHeaders(HttpContext httpContext)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    protected internal virtual Task WriteHttpResultAsync(HttpContext httpContext)
        => httpContext.Response.WriteAsJsonAsync(Value, Value!.GetType(), options: null, contentType: ContentType);

    private static int? ApplyProblemDetailsDefaults(ProblemDetails problemDetails, int? statusCode)
    {
        // We allow StatusCode to be specified either on ProblemDetails or on the ObjectResult and use it to configure the other.
        // This lets users write <c>return Conflict(new Problem("some description"))</c>
        // or <c>return Problem("some-problem", 422)</c> and have the response have consistent fields.
        if (problemDetails.Status is null)
        {
            if (statusCode is not null)
            {
                problemDetails.Status = statusCode;
            }
            else
            {
                problemDetails.Status = problemDetails is HttpValidationProblemDetails ?
                    StatusCodes.Status400BadRequest :
                    StatusCodes.Status500InternalServerError;
            }
        }

        if (ProblemDetailsDefaults.Defaults.TryGetValue(problemDetails.Status.Value, out var defaults))
        {
            problemDetails.Title ??= defaults.Title;
            problemDetails.Type ??= defaults.Type;
        }

        return statusCode ?? problemDetails.Status;
    }

    private static partial class Log
    {
        public static void ObjectResultExecuting(ILogger logger, object? value, int? statusCode)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (value is null)
                {
                    ObjectResultExecutingWithoutValue(logger, statusCode ?? StatusCodes.Status200OK);
                }
                else
                {
                    var valueType = value.GetType().FullName!;
                    ObjectResultExecuting(logger, valueType, statusCode ?? StatusCodes.Status200OK);
                }
            }
        }

        [LoggerMessage(1, LogLevel.Information, "Writing value of type '{Type}' with status code '{StatusCode}'.", EventName = "ObjectResultExecuting", SkipEnabledCheck = true)]
        private static partial void ObjectResultExecuting(ILogger logger, string type, int statusCode);

        [LoggerMessage(2, LogLevel.Information, "Executing result with status code '{StatusCode}'.", EventName = "ObjectResultExecutingWithoutValue", SkipEnabledCheck = true)]
        private static partial void ObjectResultExecutingWithoutValue(ILogger logger, int statusCode);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result;

/// <summary>
/// An action result which formats the given object as JSON.
/// </summary>
internal sealed partial class JsonResult : IResult
{
    /// <summary>
    /// Gets or sets the <see cref="Net.Http.Headers.MediaTypeHeaderValue"/> representing the Content-Type header of the response.
    /// </summary>
    public string? ContentType { get; init; }

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
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Gets or sets the value to be formatted.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Write the result as JSON to the HTTP response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    Task IResult.ExecuteAsync(HttpContext httpContext)
    {
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<JsonResult>>();
        Log.JsonResultExecuting(logger, Value);

        if (StatusCode is int statusCode)
        {
            httpContext.Response.StatusCode = statusCode;
        }

        return httpContext.Response.WriteAsJsonAsync(Value, JsonSerializerOptions, ContentType);
    }

    private static partial class Log
    {
        public static void JsonResultExecuting(ILogger logger, object? value)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var type = value == null ? "null" : value.GetType().FullName!;
                JsonResultExecuting(logger, type);
            }
        }

        [LoggerMessage(1, LogLevel.Information,
            "Executing JsonResult, writing value of type '{Type}'.",
            EventName = "JsonResultExecuting",
            SkipEnabledCheck = true)]
        private static partial void JsonResultExecuting(ILogger logger, string type);
    }
}

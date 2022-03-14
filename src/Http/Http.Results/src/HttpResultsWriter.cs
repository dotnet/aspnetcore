// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

internal static partial class HttpResultsWriter
{
    public static void WriteResultAsStatusCode(HttpContext httpContext, int statusCode)
    {
        Log.StatusCodeResultExecuting(GetLogger(httpContext), statusCode);
        httpContext.Response.StatusCode = statusCode;
    }

    public static Task WriteResultAsJson(
        HttpContext httpContext,
        object? value,
        string? contentType = null,
        int? statusCode = null,
        JsonSerializerOptions? jsonSerializerOptions = null,
        Action<HttpContext>? responseHeader = null)
    {
        Log.ObjectResultExecuting(GetLogger(httpContext), value, statusCode);

        if (value is ProblemDetails problemDetails)
        {
            ApplyProblemDetailsDefaults(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }

        if (statusCode is { } code)
        {
            WriteResultAsStatusCode(httpContext, code);
        }

        responseHeader?.Invoke(httpContext);

        if (value is null)
        {
            return Task.CompletedTask;
        }

        return httpContext.Response.WriteAsJsonAsync(value, value.GetType(), options: jsonSerializerOptions, contentType: contentType);
    }

    public static Task WriteResultAsFileAsync(
        HttpContext httpContext,
        Func<HttpContext, RangeItemHeaderValue?, long, Task> writeOperation,
        string? fileDownloadName,
        long? fileLength,
        string contentType,
        bool enableRangeProcessing,
        DateTimeOffset? lastModified,
        EntityTagHeaderValue? entityTag)
    {
        var logger = GetLogger(httpContext);

        Log.ExecutingFileResult(logger, fileDownloadName);

        var fileResultInfo = new FileResultInfo
        {
            ContentType = contentType,
            EnableRangeProcessing = enableRangeProcessing,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName ?? string.Empty,
            LastModified = lastModified,
        };

        var (range, rangeLength, serveBody) = FileResultHelper.SetHeadersAndLog(
            httpContext,
            fileResultInfo,
            fileLength,
            enableRangeProcessing,
            lastModified,
            entityTag,
            logger);

        if (!serveBody)
        {
            return Task.CompletedTask;
        }

        if (range != null && rangeLength == 0)
        {
            return Task.CompletedTask;
        }

        if (range != null)
        {
            FileResultHelper.Log.WritingRangeToBody(logger);
        }

        return writeOperation(httpContext, range, rangeLength);
    }

    public static void ApplyProblemDetailsDefaults(ProblemDetails problemDetails, int? statusCode)
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
    }

    private static ILogger GetLogger(HttpContext httpContext)
    {
        var factory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger(typeof(HttpResultsWriter));
        return logger;
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
        public static void ExecutingFileResult(ILogger logger, string? fileDownloadName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                //TODO fix
                var fileResultType = "";// fileResult.GetType().Name;
                ExecutingFileResultWithNoFileName(logger, fileResultType, fileDownloadName: fileDownloadName ?? string.Empty);
            }
        }

        public static void ExecutingFileResult(ILogger logger, string? fileDownloadName, string fileName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                //TODO fix
                var fileResultType = "";//fileResult.GetType().Name;
                ExecutingFileResult(logger, fileResultType, fileName, fileDownloadName: fileDownloadName ?? string.Empty);
            }
        }

        [LoggerMessage(1, LogLevel.Information,
            "Executing StatusCodeResult, setting HTTP status code {StatusCode}.",
            EventName = "StatusCodeResultExecuting")]
        public static partial void StatusCodeResultExecuting(ILogger logger, int statusCode);

        [LoggerMessage(2, LogLevel.Information, "Writing value of type '{Type}' with status code '{StatusCode}'.", EventName = "ObjectResultExecuting", SkipEnabledCheck = true)]
        private static partial void ObjectResultExecuting(ILogger logger, string type, int statusCode);

        [LoggerMessage(3, LogLevel.Information, "Executing result with status code '{StatusCode}'.", EventName = "ObjectResultExecutingWithoutValue", SkipEnabledCheck = true)]
        private static partial void ObjectResultExecutingWithoutValue(ILogger logger, int statusCode);

        [LoggerMessage(4, LogLevel.Information,
            "Executing {FileResultType}, sending file with download name '{FileDownloadName}'.",
            EventName = "ExecutingFileResultWithNoFileName",
            SkipEnabledCheck = true)]
        private static partial void ExecutingFileResultWithNoFileName(ILogger logger, string fileResultType, string fileDownloadName);

        [LoggerMessage(5, LogLevel.Information,
            "Executing {FileResultType}, sending file '{FileDownloadPath}' with download name '{FileDownloadName}'.",
            EventName = "ExecutingFileResult",
            SkipEnabledCheck = true)]
        private static partial void ExecutingFileResult(ILogger logger, string fileResultType, string fileDownloadPath, string fileDownloadName);
    }
}

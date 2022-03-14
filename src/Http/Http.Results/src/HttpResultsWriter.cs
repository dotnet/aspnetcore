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
    public static void WriteResultAsStatusCode(
        HttpContext httpContext,
        IStatusCodeHttpResult statusCodeHttpResult)
    {
        if (statusCodeHttpResult is { StatusCode: { } statusCode })
        {
            Log.WritingResultAsStatusCode(GetLogger(httpContext), statusCodeResultType: statusCodeHttpResult.GetType().Name, statusCode);
            httpContext.Response.StatusCode = statusCode;
        }
    }

    public static Task WriteResultAsJson(
        HttpContext httpContext,
        IObjectHttpResult objectHttpResult,
        string? contentType = null,
        JsonSerializerOptions? jsonSerializerOptions = null,
        Action<HttpContext>? configureResponseHeader = null)
    {
        Log.WritingResultAsJson(GetLogger(httpContext), objectHttpResult);

        var statusCode = objectHttpResult.StatusCode;

        if (objectHttpResult.Value is ProblemDetails problemDetails)
        {
            ApplyProblemDetailsDefaults(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }

        if (statusCode is { } code)
        {
            httpContext.Response.StatusCode = code;
        }

        configureResponseHeader?.Invoke(httpContext);

        if (objectHttpResult.Value is null)
        {
            return Task.CompletedTask;
        }

        return httpContext.Response.WriteAsJsonAsync(
            objectHttpResult.Value,
            objectHttpResult.Value.GetType(),
            options: jsonSerializerOptions,
            contentType: contentType);
    }

    public static Task WriteResultAsFileAsync(
        HttpContext httpContext,
        IFileHttpResult fileHttpResult,
        Func<HttpContext, RangeItemHeaderValue?, long, Task> writeOperation)
    {
        var logger = GetLogger(httpContext);

        Log.WritingResultAsFile(logger, fileHttpResult);

        var fileResultInfo = new FileResultInfo
        {
            ContentType = fileHttpResult.ContentType,
            EnableRangeProcessing = fileHttpResult.EnableRangeProcessing,
            EntityTag = fileHttpResult.EntityTag,
            FileDownloadName = fileHttpResult.FileDownloadName ?? string.Empty,
            LastModified = fileHttpResult.LastModified,
        };

        var (range, rangeLength, serveBody) = FileResultHelper.SetHeadersAndLog(
            httpContext,
            fileResultInfo,
            fileHttpResult.FileLength,
            fileHttpResult.EnableRangeProcessing,
            fileHttpResult.LastModified,
            fileHttpResult.EntityTag,
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
        public static void WritingResultAsJson(ILogger logger, IObjectHttpResult objectHttpResult)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var objectResultType = objectHttpResult.GetType().Name;

                if (objectHttpResult.Value is null)
                {
                    WritingResultAsJsonWithoutValue(logger, objectResultType, objectHttpResult.StatusCode ?? StatusCodes.Status200OK);
                }
                else
                {
                    var valueType = objectHttpResult.Value.GetType().FullName!;
                    WritingResultAsJson(logger, objectResultType, valueType, objectHttpResult.StatusCode ?? StatusCodes.Status200OK);
                }
            }
        }
        public static void WritingResultAsFile(ILogger logger, IFileHttpResult fileHttpResult)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var fileResultType = fileHttpResult.GetType().Name;
                WritingResultAsFileWithNoFileName(logger, fileResultType, fileDownloadName: fileHttpResult.FileDownloadName ?? string.Empty);
            }
        }

        [LoggerMessage(1, LogLevel.Information,
            "Executing '{StatusCodeResultType}', setting HTTP status code {StatusCode}.",
            EventName = "StatusCodeResultExecuting")]
        public static partial void WritingResultAsStatusCode(ILogger logger, string statusCodeResultType, int statusCode);

        [LoggerMessage(2, LogLevel.Information, "Executing '{ObjectResultType}', writing value of type '{Type}' with status code '{StatusCode}'.", EventName = "ObjectResultExecuting", SkipEnabledCheck = true)]
        private static partial void WritingResultAsJson(ILogger logger, string objectResultType, string type,  int statusCode);

        [LoggerMessage(3, LogLevel.Information, "Executing result '{ObjectResultType}' with status code '{StatusCode}'.", EventName = "ObjectResultExecutingWithoutValue", SkipEnabledCheck = true)]
        private static partial void WritingResultAsJsonWithoutValue(ILogger logger, string objectResultType, int statusCode);

        [LoggerMessage(4, LogLevel.Information,
            "Executing {FileResultType}, sending file with download name '{FileDownloadName}'.",
            EventName = "ExecutingFileResultWithNoFileName",
            SkipEnabledCheck = true)]
        private static partial void WritingResultAsFileWithNoFileName(ILogger logger, string fileResultType, string fileDownloadName);
    }
}

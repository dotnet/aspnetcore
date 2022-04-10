// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal static partial class HttpResultsHelper
{
    private const string DefaultContentType = "text/plain; charset=utf-8";
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    public static Task WriteResultAsJsonAsync<T>(
        HttpContext httpContext,
        ILogger logger,
        T? value,
        string? contentType = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        Log.WritingResultAsJson(logger, typeof(T).Name);

        return httpContext.Response.WriteAsJsonAsync(
            value,
            value.GetType(),
            options: jsonSerializerOptions,
            contentType: contentType);
    }

    public static Task WriteResultAsContentAsync(
        HttpContext httpContext,
        ILogger logger,
        string? content,
        string? contentType = null)
    {
        var response = httpContext.Response;
        ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
            contentType,
            response.ContentType,
            (DefaultContentType, DefaultEncoding),
            ResponseContentTypeHelper.GetEncoding,
            out var resolvedContentType,
            out var resolvedContentTypeEncoding);

        response.ContentType = resolvedContentType;

        Log.WritingResultAsContent(logger, resolvedContentType);

        if (content != null)
        {
            response.ContentLength = resolvedContentTypeEncoding.GetByteCount(content);
            return response.WriteAsync(content, resolvedContentTypeEncoding);
        }

        return Task.CompletedTask;
    }

    public static (RangeItemHeaderValue? range, long rangeLength, bool completed) WriteResultAsFileCore(
        HttpContext httpContext,
        ILogger logger,
        string? fileDownloadName,
        long? fileLength,
        string contentType,
        bool enableRangeProcessing,
        DateTimeOffset? lastModified,
        EntityTagHeaderValue? entityTag)
    {
        var completed = false;
        fileDownloadName ??= string.Empty;

        Log.WritingResultAsFile(logger, fileDownloadName);

        var fileResultInfo = new FileResultInfo
        {
            ContentType = contentType,
            EnableRangeProcessing = enableRangeProcessing,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
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

        if (range != null)
        {
            FileResultHelper.Log.WritingRangeToBody(logger);
        }

        if (!serveBody)
        {
            completed = true;
        }

        if (range != null && rangeLength == 0)
        {
            completed = true;
        }

        return (range, rangeLength, completed);
    }

    public static void ApplyProblemDetailsDefaultsIfNeeded(object? value, int? statusCode)
    {
        if (value is ProblemDetails problemDetails)
        {
            ApplyProblemDetailsDefaults(problemDetails, statusCode);
        }
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

    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information,
            "Setting HTTP status code {StatusCode}.",
            EventName = "WritingResultAsStatusCode")]
        public static partial void WritingResultAsStatusCode(ILogger logger, int statusCode);

        [LoggerMessage(2, LogLevel.Information,
            "Write content with HTTP Response ContentType of {ContentType}",
            EventName = "WritingResultAsContent")]
        public static partial void WritingResultAsContent(ILogger logger, string contentType);

        [LoggerMessage(3, LogLevel.Information, "Writing value of type '{Type}' as Json.",
            EventName = "WritingResultAsJson")]
        public static partial void WritingResultAsJson(ILogger logger, string type);

        [LoggerMessage(5, LogLevel.Information,
            "Sending file with download name '{FileDownloadName}'.",
            EventName = "WritingResultAsFileWithNoFileName")]
        public static partial void WritingResultAsFile(ILogger logger, string fileDownloadName);
    }
}

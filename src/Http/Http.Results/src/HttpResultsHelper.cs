// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal static partial class HttpResultsHelper
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed ASP.NET apps, ensures the JsonSerializer doesn't use Reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
    public static Task WriteResultAsJsonAsync<TValue>(
        HttpContext httpContext,
        ILogger logger,
        TValue? value,
        string? contentType = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        jsonSerializerOptions ??= ResolveJsonOptions(httpContext).SerializerOptions;
        var jsonTypeInfo = (JsonTypeInfo<TValue>)jsonSerializerOptions.GetTypeInfo(typeof(TValue));

        Type? runtimeType = value.GetType();
        if (jsonTypeInfo.ShouldUseWith(runtimeType))
        {
            Log.WritingResultAsJson(logger, jsonTypeInfo.Type.Name);
            return httpContext.Response.WriteAsJsonAsync(
                value,
                jsonTypeInfo,
                contentType: contentType);
        }

        Log.WritingResultAsJson(logger, runtimeType.Name);
        // Since we don't know the type's polymorphic characteristics
        // our best option is to serialize the value as 'object'.
        // call WriteAsJsonAsync<object>() rather than the declared type
        // and avoid source generators issues.
        // https://github.com/dotnet/aspnetcore/issues/43894
        // https://learn.microsoft.com/dotnet/standard/serialization/system-text-json-polymorphism
        return httpContext.Response.WriteAsJsonAsync<object>(
           value,
           jsonSerializerOptions,
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
            (ContentTypeConstants.DefaultContentType, DefaultEncoding),
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
            ProblemDetailsDefaults.Apply(problemDetails, statusCode);
        }
    }

    private static JsonOptions ResolveJsonOptions(HttpContext httpContext)
    {
        // Attempt to resolve options from DI then fallback to default options
        return httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions();
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

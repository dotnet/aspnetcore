// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file as the response.
/// </summary>
public abstract partial class FileHttpResult : IResult
{
    private string? _fileDownloadName;

    internal FileHttpResult(string? contentType)
    {
        ContentType = contentType ?? "application/octet-stream";
    }

    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the file name that will be used in the Content-Disposition header of the response.
    /// </summary>
    [AllowNull]
    public string FileDownloadName
    {
        get { return _fileDownloadName ?? string.Empty; }
        internal init { _fileDownloadName = value; }
    }

    /// <summary>
    /// Gets or sets the last modified information associated with the <see cref="FileHttpResult"/>.
    /// </summary>
    public DateTimeOffset? LastModified { get; internal set; }

    /// <summary>
    /// Gets or sets the etag associated with the <see cref="FileHttpResult"/>.
    /// </summary>
    public EntityTagHeaderValue? EntityTag { get; internal init; }

    /// <summary>
    /// Gets or sets the value that enables range processing for the <see cref="FileHttpResult"/>.
    /// </summary>
    public bool EnableRangeProcessing { get; internal init; }

    /// <summary>
    /// Gets or sets the file length information associated with the <see cref="FileHttpResult"/>.
    /// </summary>
    public long? FileLength { get; internal set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    protected internal abstract ILogger GetLogger(HttpContext httpContext);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="range"></param>
    /// <param name="rangeLength"></param>
    /// <returns></returns>
    protected internal abstract Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public virtual Task ExecuteAsync(HttpContext httpContext)
    {
        var logger = GetLogger(httpContext);

        Log.ExecutingFileResult(logger, this);

        var fileResultInfo = new FileResultInfo
        {
            ContentType = ContentType,
            EnableRangeProcessing = EnableRangeProcessing,
            EntityTag = EntityTag,
            FileDownloadName = FileDownloadName,
            LastModified = LastModified,
        };

        var (range, rangeLength, serveBody) = FileResultHelper.SetHeadersAndLog(
            httpContext,
            fileResultInfo,
            FileLength,
            EnableRangeProcessing,
            LastModified,
            EntityTag,
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

        return ExecuteCoreAsync(httpContext, range, rangeLength);
    }

    internal static partial class Log
    {
        public static void ExecutingFileResult(ILogger logger, FileHttpResult fileResult)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var fileResultType = fileResult.GetType().Name;
                ExecutingFileResultWithNoFileName(logger, fileResultType, fileResult.FileDownloadName);
            }
        }

        public static void ExecutingFileResult(ILogger logger, FileHttpResult fileResult, string fileName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var fileResultType = fileResult.GetType().Name;
                ExecutingFileResult(logger, fileResultType, fileName, fileResult.FileDownloadName);
            }
        }

        [LoggerMessage(1, LogLevel.Information,
            "Executing {FileResultType}, sending file with download name '{FileDownloadName}'.",
            EventName = "ExecutingFileResultWithNoFileName",
            SkipEnabledCheck = true)]
        private static partial void ExecutingFileResultWithNoFileName(ILogger logger, string fileResultType, string fileDownloadName);

        [LoggerMessage(2, LogLevel.Information,
            "Executing {FileResultType}, sending file '{FileDownloadPath}' with download name '{FileDownloadName}'.",
            EventName = "ExecutingFileResult",
            SkipEnabledCheck = true)]
        private static partial void ExecutingFileResult(ILogger logger, string fileResultType, string fileDownloadPath, string fileDownloadName);
    }
}

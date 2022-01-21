// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{PhysicalFileResult}"/> for <see cref="PhysicalFileResult"/>.
/// </summary>
public partial class PhysicalFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<PhysicalFileResult>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PhysicalFileResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    public PhysicalFileResultExecutor(ILoggerFactory loggerFactory)
        : base(CreateLogger<PhysicalFileResultExecutor>(loggerFactory))
    {
    }

    /// <inheritdoc />
    public virtual Task ExecuteAsync(ActionContext context, PhysicalFileResult result)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var fileInfo = GetFileInfo(result.FileName);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(
                Resources.FormatFileResult_InvalidPath(result.FileName), result.FileName);
        }

        Logger.ExecutingFileResult(result, result.FileName);

        var lastModified = result.LastModified ?? fileInfo.LastModified;
        var (range, rangeLength, serveBody) = SetHeadersAndLog(
            context,
            result,
            fileInfo.Length,
            result.EnableRangeProcessing,
            lastModified,
            result.EntityTag);

        if (serveBody)
        {
            return WriteFileAsync(context, result, range, rangeLength);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected virtual Task WriteFileAsync(ActionContext context, PhysicalFileResult result, RangeItemHeaderValue? range, long rangeLength)
    {
        return WriteFileAsyncInternal(context.HttpContext, result, range, rangeLength, Logger);
    }

    internal static Task WriteFileAsyncInternal(
        HttpContext httpContext,
        PhysicalFileResult result,
        RangeItemHeaderValue? range,
        long rangeLength,
        ILogger logger)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (range != null && rangeLength == 0)
        {
            return Task.CompletedTask;
        }

        var response = httpContext.Response;
        if (!Path.IsPathRooted(result.FileName))
        {
            throw new NotSupportedException(Resources.FormatFileResult_PathNotRooted(result.FileName));
        }

        if (range != null)
        {
            Log.WritingRangeToBody(logger);
        }

        if (range != null)
        {
            return response.SendFileAsync(result.FileName,
                offset: range.From ?? 0L,
                count: rangeLength);
        }

        return response.SendFileAsync(result.FileName,
            offset: 0,
            count: null);
    }

    /// <summary>
    /// Obsolete. This API is no longer called.
    /// </summary>
    [Obsolete("This API is no longer called.")]
    protected virtual Stream GetFileStream(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
    }


    /// <summary>
    /// Get the file metadata for a path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The <see cref="FileMetadata"/> for the path.</returns>
    protected virtual FileMetadata GetFileInfo(string path)
    {
        var fileInfo = new FileInfo(path);

        // It means we are dealing with a symlink and need to get the information
        // from the target file instead.
        if (fileInfo.Exists && !string.IsNullOrEmpty(fileInfo.LinkTarget))
        {
            fileInfo = (FileInfo?)fileInfo.ResolveLinkTarget(returnFinalTarget: true) ?? fileInfo;
        }

        return new FileMetadata
        {
            Exists = fileInfo.Exists,
            Length = fileInfo.Length,
            LastModified = fileInfo.LastWriteTimeUtc,
        };
    }

    /// <summary>
    /// Represents metadata for a file.
    /// </summary>
    protected class FileMetadata
    {
        /// <summary>
        /// Whether a file exists.
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// The file length.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// When the file was last modified.
        /// </summary>
        public DateTimeOffset LastModified { get; set; }
    }

    private static partial class Log
    {
        [LoggerMessage(17, LogLevel.Debug, "Writing the requested range of bytes to the body...", EventName = "WritingRangeToBody")]
        public static partial void WritingRangeToBody(ILogger logger);
    }
}

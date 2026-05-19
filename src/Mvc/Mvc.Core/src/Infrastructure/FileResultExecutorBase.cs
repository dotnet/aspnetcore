// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Base class for executing a file result.
/// </summary>
public class FileResultExecutorBase
{
    /// <summary>
    /// The buffer size: 64 * 1024.
    /// </summary>
    protected const int BufferSize = 64 * 1024;

    /// <summary>
    /// Intializes a new <see cref="FileResultExecutorBase"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public FileResultExecutorBase(ILogger logger)
    {
        Logger = logger;
    }

    internal enum PreconditionState
    {
        Unspecified,
        NotModified,
        ShouldProcess,
        PreconditionFailed
    }

    /// <summary>
    /// The logger to use.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Sets etag and last modified headers.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="result">The <see cref="FileResult"/>.</param>
    /// <param name="fileLength">The nullable file length.</param>
    /// <param name="enableRangeProcessing">Whether range processing is enabled.</param>
    /// <param name="lastModified">The nullable lastModified date.</param>
    /// <param name="etag">The <see cref="EntityTagHeaderValue"/>.</param>
    /// <returns>A tuple with the <see cref="RangeItemHeaderValue"/> range, length, and whether the body was served.</returns>
    protected virtual (RangeItemHeaderValue? range, long rangeLength, bool serveBody) SetHeadersAndLog(
        ActionContext context,
        FileResult result,
        long? fileLength,
        bool enableRangeProcessing,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? etag = null)
    {
        var fileResultInfo = new FileResultInfo
        {
            ContentType = result.ContentType,
            EnableRangeProcessing = result.EnableRangeProcessing,
            EntityTag = result.EntityTag,
            FileDownloadName = result.FileDownloadName,
            LastModified = result.LastModified,
        };

        return FileResultHelper.SetHeadersAndLog(context.HttpContext, fileResultInfo, fileLength, enableRangeProcessing, lastModified, etag, Logger);
    }

    /// <summary>
    /// Creates a logger using the factory.
    /// </summary>
    /// <typeparam name="T">The type being logged.</typeparam>
    /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
    /// <returns>An <see cref="ILogger"/>.</returns>
    protected static ILogger CreateLogger<T>(ILoggerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.CreateLogger<T>();
    }

    /// <summary>
    /// Write the contents of the fileStream to the response body.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="fileStream">The fileStream to write.</param>
    /// <param name="range">The <see cref="RangeItemHeaderValue"/>.</param>
    /// <param name="rangeLength">The range length.</param>
    /// <returns>The async task.</returns>
    protected static async Task WriteFileAsync(HttpContext context, Stream fileStream, RangeItemHeaderValue? range, long rangeLength)
    {
        await FileResultHelper.WriteFileAsync(context, fileStream, range, rangeLength);
    }
}

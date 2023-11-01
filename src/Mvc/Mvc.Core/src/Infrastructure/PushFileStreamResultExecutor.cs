// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{PushFileStreamResult}"/> for <see cref="PushFileStreamResult"/>.
/// </summary>
public partial class PushFileStreamResultExecutor : FileResultExecutorBase, IActionResultExecutor<PushFileStreamResult>
{
    /// <summary>
    /// Initializes a new <see cref="PushFileStreamResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    public PushFileStreamResultExecutor(ILoggerFactory loggerFactory)
        : base(CreateLogger<PushFileStreamResultExecutor>(loggerFactory))
    {
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAsync(ActionContext context, PushFileStreamResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        Log.ExecutingFileResult(Logger, result);

        var (range, rangeLength, serveBody) = SetHeadersAndLog(
            context,
            result,
            fileLength: null,
            result.EnableRangeProcessing,
            result.LastModified,
            result.EntityTag);

        if (!serveBody)
        {
            return;
        }

        await WriteFileAsync(context, result, range, rangeLength);
    }

    /// <summary>
    /// Write the contents of the PushFileStreamResult to the response body.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="result">The PushFileStreamResult to write.</param>
    /// <param name="range">The <see cref="RangeItemHeaderValue"/>.</param>
    /// <param name="rangeLength">The range length.</param>
    protected virtual async Task WriteFileAsync(
        ActionContext context,
        PushFileStreamResult result,
        RangeItemHeaderValue? range,
        long rangeLength)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        Debug.Assert(range == null);
        Debug.Assert(rangeLength == 0);

        await result.StreamWriterCallback(context.HttpContext.Response.Body);
    }

    private static partial class Log
    {
        public static void ExecutingFileResult(ILogger logger, FileResult fileResult)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var fileResultType = fileResult.GetType().Name;
                ExecutingFileResultWithNoFileName(logger, fileResultType, fileResult.FileDownloadName);
            }
        }

        [LoggerMessage(1, LogLevel.Information, "Executing {FileResultType}, sending file with download name '{FileDownloadName}' ...", EventName = "ExecutingFileResultWithNoFileName", SkipEnabledCheck = true)]
        private static partial void ExecutingFileResultWithNoFileName(ILogger logger, string fileResultType, string fileDownloadName);
    }
}

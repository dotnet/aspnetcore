// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

internal abstract class FileResult : FileResultBase, IResult
{
    public FileResult(string? contentType)
        : base(contentType)
    {
    }

    protected abstract ILogger GetLogger(HttpContext httpContext);

    protected abstract Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength);

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
}

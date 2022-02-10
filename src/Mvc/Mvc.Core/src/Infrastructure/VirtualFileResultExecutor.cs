// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{VirtualFileResult}"/> for <see cref="VirtualFileResult"/>.
/// </summary>
public partial class VirtualFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<VirtualFileResult>
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    /// <summary>
    /// Initializes a new instance of <see cref="VirtualFileResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    /// <param name="hostingEnvironment">The hosting environment</param>
    public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment)
        : base(CreateLogger<VirtualFileResultExecutor>(loggerFactory))
    {
        if (hostingEnvironment == null)
        {
            throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        _hostingEnvironment = hostingEnvironment;
    }

    /// <inheritdoc />
    public virtual Task ExecuteAsync(ActionContext context, VirtualFileResult result)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var fileInfo = GetFileInformation(result, _hostingEnvironment);
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
            return WriteFileAsync(context, result, fileInfo, range, rangeLength);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected virtual Task WriteFileAsync(ActionContext context, VirtualFileResult result, IFileInfo fileInfo, RangeItemHeaderValue? range, long rangeLength)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return WriteFileAsyncInternal(context.HttpContext, fileInfo, range, rangeLength, Logger);
    }

    internal static Task WriteFileAsyncInternal(
        HttpContext httpContext,
        IFileInfo fileInfo,
        RangeItemHeaderValue? range,
        long rangeLength,
        ILogger logger)
    {
        if (range != null && rangeLength == 0)
        {
            return Task.CompletedTask;
        }

        var response = httpContext.Response;

        if (range != null)
        {
            Log.WritingRangeToBody(logger);
        }

        if (range != null)
        {
            return response.SendFileAsync(fileInfo,
                offset: range.From ?? 0L,
                count: rangeLength);
        }

        return response.SendFileAsync(fileInfo,
            offset: 0,
            count: null);
    }

    internal static IFileInfo GetFileInformation(VirtualFileResult result, IWebHostEnvironment hostingEnvironment)
    {
        var fileProvider = GetFileProvider(result, hostingEnvironment);
        if (fileProvider is NullFileProvider)
        {
            throw new InvalidOperationException(Resources.VirtualFileResultExecutor_NoFileProviderConfigured);
        }

        var normalizedPath = result.FileName;
        if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        var fileInfo = fileProvider.GetFileInfo(normalizedPath);
        return fileInfo;
    }

    internal static IFileProvider GetFileProvider(VirtualFileResult result, IWebHostEnvironment hostingEnvironment)
    {
        if (result.FileProvider != null)
        {
            return result.FileProvider;
        }

        result.FileProvider = hostingEnvironment.WebRootFileProvider;
        return result.FileProvider;
    }

    /// <summary>
    /// Obsolete, this API is no longer called.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    [Obsolete("This API is no longer called.")]
    protected virtual Stream GetFileStream(IFileInfo fileInfo)
    {
        return fileInfo.CreateReadStream();
    }

    private static partial class Log
    {
        [LoggerMessage(17, LogLevel.Debug, "Writing the requested range of bytes to the body...", EventName = "WritingRangeToBody")]
        public static partial void WritingRangeToBody(ILogger logger);
    }
}

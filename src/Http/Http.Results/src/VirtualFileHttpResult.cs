// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A <see cref="IResult" /> that on execution writes the file specified
/// using a virtual path to the response using mechanisms provided by the host.
/// </summary>
public sealed class VirtualFileHttpResult : IResult
{
    private DateTimeOffset? _lastModified;

    /// <summary>
    /// Creates a new <see cref="VirtualFileHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    public VirtualFileHttpResult(string fileName)
        : this(fileName, contentType: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="VirtualFileHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public VirtualFileHttpResult(string fileName, string? contentType)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? "application/octet-stream";
    }

    /// <summary>
    /// Gets or sets the path to the file that will be sent back as the response.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets or sets the file length information .
    /// </summary>
    public long? FileLength { get; private set; }

    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the value that enables range processing for the file result.
    /// </summary>
    public bool EnableRangeProcessing { get; init; }

    /// <summary>
    /// Gets the etag associated with the file result.
    /// </summary>
    public EntityTagHeaderValue? EntityTag { get; init; }

    /// <summary>
    /// Gets the file name that will be used in the Content-Disposition header of the response.
    /// </summary>
    public string? FileDownloadName { get; init; }

    /// <summary>
    /// Gets the last modified information associated with the file result.
    /// </summary>
    public DateTimeOffset? LastModified
    {
        get => _lastModified;
        init => _lastModified = value;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var hostingEnvironment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        var fileInfo = GetFileInformation(hostingEnvironment.WebRootFileProvider);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Could not find file: {FileName}.", FileName);
        }
        _lastModified = LastModified ?? fileInfo.LastModified;
        FileLength = fileInfo.Length;

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.VirtualFileResult");

        var (range, rangeLength, completed) = HttpResultsHelper.WriteResultAsFileCore(
            httpContext,
            logger,
            FileDownloadName,
            FileLength,
            ContentType,
            EnableRangeProcessing,
            LastModified,
            EntityTag);

        return completed ?
            Task.CompletedTask :
            ExecuteCoreAsync(httpContext, range, rangeLength, fileInfo);
    }

    private static Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength, IFileInfo fileInfo)
    {
        var response = httpContext.Response;
        var offset = 0L;
        var count = (long?)null;
        if (range != null)
        {
            offset = range.From ?? 0L;
            count = rangeLength;
        }

        return response.SendFileAsync(
            fileInfo!,
            offset,
            count);
    }

    internal IFileInfo GetFileInformation(IFileProvider fileProvider)
    {
        var normalizedPath = FileName;
        if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        var fileInfo = fileProvider.GetFileInfo(normalizedPath);
        return fileInfo;
    }
}

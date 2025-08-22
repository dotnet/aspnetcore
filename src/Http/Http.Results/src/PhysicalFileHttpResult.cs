// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// A <see cref="PhysicalFileHttpResult"/> on execution will write a file from disk to the response
/// using mechanisms provided by the host.
/// </summary>
public sealed partial class PhysicalFileHttpResult : IResult, IFileHttpResult, IContentTypeHttpResult
{
    /// <summary>
    /// Creates a new <see cref="PhysicalFileHttpResult"/> instance with
    /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    internal PhysicalFileHttpResult(string fileName, string? contentType)
        : this(fileName, contentType, fileDownloadName: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="PhysicalFileHttpResult"/> instance with
    /// the provided <paramref name="fileName"/>, the provided <paramref name="contentType"/>
    /// and the provided <paramref name="fileDownloadName"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    internal PhysicalFileHttpResult(
        string fileName,
        string? contentType,
        string? fileDownloadName)
        : this(fileName, contentType, fileDownloadName, enableRangeProcessing: false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="PhysicalFileHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    internal PhysicalFileHttpResult(
        string fileName,
        string? contentType,
        string? fileDownloadName,
        bool enableRangeProcessing,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
    {
        FileName = fileName;
        ContentType = contentType ?? ContentTypeConstants.BinaryContentType;
        FileDownloadName = fileDownloadName;
        EnableRangeProcessing = enableRangeProcessing;
        LastModified = lastModified;
        EntityTag = entityTag;
    }

    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    public string ContentType { get; internal set; }

    /// <summary>
    /// Gets the file name that will be used in the Content-Disposition header of the response.
    /// </summary>
    public string? FileDownloadName { get; internal set; }

    /// <summary>
    /// Gets the last modified information associated with the file result.
    /// </summary>
    public DateTimeOffset? LastModified { get; internal set; }

    /// <summary>
    /// Gets the etag associated with the file result.
    /// </summary>
    public EntityTagHeaderValue? EntityTag { get; internal init; }

    /// <summary>
    /// Gets the value that enables range processing for the file result.
    /// </summary>
    public bool EnableRangeProcessing { get; internal init; }

    /// <summary>
    /// Gets or sets the file length information .
    /// </summary>
    public long? FileLength { get; internal set; }

    /// <summary>
    /// Gets or sets the path to the file that will be sent back as the response.
    /// </summary>
    public string FileName { get; }

    // For testing
    internal Func<string, FileInfoWrapper> GetFileInfoWrapper { get; init; } =
        static path => new FileInfoWrapper(path);

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var fileInfo = GetFileInfoWrapper(FileName);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Could not find file: {FileName}", FileName);
        }

        LastModified ??= fileInfo.LastWriteTimeUtc;
        FileLength = fileInfo.Length;

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.PhysicalFileResult");

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
            ExecuteCoreAsync(httpContext, range, rangeLength, FileName);
    }

    private static Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength, string fileName)
    {
        var response = httpContext.Response;
        if (!Path.IsPathRooted(fileName))
        {
            throw new NotSupportedException($"Path '{fileName}' was not rooted.");
        }

        var offset = 0L;
        var count = (long?)null;
        if (range != null)
        {
            offset = range.From ?? 0L;
            count = rangeLength;
        }

        return response.SendFileAsync(
            fileName,
            offset: offset,
            count: count);
    }

    internal readonly struct FileInfoWrapper
    {
        public FileInfoWrapper(string path)
        {
            var fileInfo = new FileInfo(path);

            // It means we are dealing with a symlink and need to get the information
            // from the target file instead.
            if (fileInfo.Exists && !string.IsNullOrEmpty(fileInfo.LinkTarget))
            {
                fileInfo = (FileInfo?)fileInfo.ResolveLinkTarget(returnFinalTarget: true) ?? fileInfo;
            }

            Exists = fileInfo.Exists;
            Length = fileInfo.Length;
            LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
        }

        public bool Exists { get; init; }

        public long Length { get; init; }

        public DateTimeOffset LastWriteTimeUtc { get; init; }
    }
}

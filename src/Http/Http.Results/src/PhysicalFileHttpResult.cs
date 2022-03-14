// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A <see cref="PhysicalFileHttpResult"/> on execution will write a file from disk to the response
/// using mechanisms provided by the host.
/// </summary>
public sealed partial class PhysicalFileHttpResult : IResult, IFileHttpResult
{
    /// <summary>
    /// Creates a new <see cref="PhysicalFileHttpResult"/> instance with
    /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    internal PhysicalFileHttpResult(string fileName, string? contentType)
    {
        FileName = fileName;
        ContentType = contentType ?? "application/octet-stream";
    }

    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    public string ContentType { get; internal set;}

    /// <summary>
    /// Gets the file name that will be used in the Content-Disposition header of the response.
    /// </summary>
    public string? FileDownloadName { get; internal set;}

    /// <summary>
    /// Gets or sets the last modified information associated with the <see cref="IFileHttpResult"/>.
    /// </summary>
    public DateTimeOffset? LastModified { get; internal set; }

    /// <summary>
    /// Gets or sets the etag associated with the <see cref="FileHttpResult"/>.
    /// </summary>
    public EntityTagHeaderValue? EntityTag { get; internal init; }

    /// <summary>
    /// Gets or sets the value that enables range processing for the <see cref="IFileHttpResult"/>.
    /// </summary>
    public bool EnableRangeProcessing { get; internal init; }

    /// <summary>
    /// Gets or sets the file length information associated with the <see cref="IFileHttpResult"/>.
    /// </summary>
    public long? FileLength { get; internal set; }

    /// <summary>
    /// Gets or sets the path to the file that will be sent back as the response.
    /// </summary>
    public string FileName { get; }

    // For testing
    internal Func<string, FileInfoWrapper> GetFileInfoWrapper { get; init; } =
        static path => new FileInfoWrapper(path);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var fileInfo = GetFileInfoWrapper(FileName);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Could not find file: {FileName}", FileName);
        }

        LastModified = LastModified ?? fileInfo.LastWriteTimeUtc;
        FileLength = fileInfo.Length;

        return HttpResultsWriter.WriteResultAsFileAsync(httpContext,
            ExecuteCoreAsync,
            FileDownloadName,
            FileLength,
            ContentType,
            EnableRangeProcessing,
            LastModified,
            EntityTag);
    }

    private Task ExecuteCoreAsync(HttpContext httpContext, RangeItemHeaderValue? range, long rangeLength)
    {
        var response = httpContext.Response;
        if (!Path.IsPathRooted(FileName))
        {
            throw new NotSupportedException($"Path '{FileName}' was not rooted.");
        }

        var offset = 0L;
        var count = (long?)null;
        if (range != null)
        {
            offset = range.From ?? 0L;
            count = rangeLength;
        }

        return response.SendFileAsync(
            FileName,
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

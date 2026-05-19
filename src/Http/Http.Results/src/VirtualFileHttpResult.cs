// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// A <see cref="IResult" /> that on execution writes the file specified
/// using a virtual path to the response using mechanisms provided by the host.
/// </summary>
public sealed class VirtualFileHttpResult : IResult, IFileHttpResult, IContentTypeHttpResult
{
    private string _fileName;

    /// <summary>
    /// Creates a new <see cref="VirtualFileHttpResult"/> instance with
    /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    internal VirtualFileHttpResult(string fileName, string? contentType)
        : this(fileName, contentType, fileDownloadName: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="VirtualFileHttpResult"/> instance with
    /// the provided <paramref name="fileName"/>, the provided <paramref name="contentType"/>
    /// and the provided <paramref name="fileDownloadName"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    internal VirtualFileHttpResult(
        string fileName,
        string? contentType,
        string? fileDownloadName)
        : this(fileName, contentType, fileDownloadName, enableRangeProcessing: false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="VirtualFileHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    internal VirtualFileHttpResult(
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

    /// <inheritdoc />
    public string ContentType { get; internal set; }

    /// <inheritdoc />
    public string? FileDownloadName { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset? LastModified { get; internal set; }

    /// <inheritdoc />
    public EntityTagHeaderValue? EntityTag { get; internal init; }

    /// <inheritdoc />
    public bool EnableRangeProcessing { get; internal init; }

    /// <inheritdoc />
    public long? FileLength { get; internal set; }

    /// <summary>
    /// Gets or sets the path to the file that will be sent back as the response.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        [MemberNotNull(nameof(_fileName))]
        internal set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var hostingEnvironment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        var fileInfo = GetFileInformation(hostingEnvironment.WebRootFileProvider);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Could not find file: {FileName}.", FileName);
        }
        LastModified = LastModified ?? fileInfo.LastModified;
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
        if (normalizedPath.StartsWith('~'))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        var fileInfo = fileProvider.GetFileInfo(normalizedPath);
        return fileInfo;
    }
}

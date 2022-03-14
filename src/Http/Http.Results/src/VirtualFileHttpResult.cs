// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A <see cref="IResult" /> that on execution writes the file specified using a virtual path to the response
/// using mechanisms provided by the host.
/// </summary>
public sealed class VirtualFileHttpResult : IResult, IFileHttpResult
{
    private string _fileName;
    private IFileInfo? _fileInfo;

    /// <summary>
    /// Creates a new <see cref="VirtualFileHttpResult"/> instance with the provided <paramref name="fileName"/>
    /// and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    internal VirtualFileHttpResult(string fileName, string? contentType)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? "application/octet-stream";
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
    /// Gets or sets the last modified information associated with the <see cref="IFileHttpResult"/>.
    /// </summary>
    public DateTimeOffset? LastModified { get; internal set; }

    /// <summary>
    /// Gets or sets the etag associated with the <see cref="IFileHttpResult"/>.
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
    public string FileName
    {
        get => _fileName;
        [MemberNotNull(nameof(_fileName))]
        set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
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

        _fileInfo = fileInfo;
        LastModified = LastModified ?? fileInfo.LastModified;
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
        var offset = 0L;
        var count = (long?)null;
        if (range != null)
        {
            offset = range.From ?? 0L;
            count = rangeLength;
        }

        return response.SendFileAsync(
            _fileInfo!,
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

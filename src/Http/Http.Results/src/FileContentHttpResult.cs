// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed partial class FileContentHttpResult : IResult, IFileHttpResult
{
    /// <summary>
    /// Creates a new <see cref="FileContentHttpResult"/> instance with
    /// the provided <paramref name="fileContents"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileContents">The bytes that represent the file contents.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    internal FileContentHttpResult(ReadOnlyMemory<byte> fileContents, string? contentType)
    {
        FileContents = fileContents;
        FileLength = fileContents.Length;
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
    /// Gets or sets the file contents.
    /// </summary>
    public ReadOnlyMemory<byte> FileContents { get; init; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return HttpResultsWriter.WriteResultAsFileAsync(httpContext,
            (context, range, rangeLength) => FileResultHelper.WriteFileAsync(httpContext, FileContents, range, rangeLength),
            FileDownloadName,
            FileLength,
            ContentType,
            EnableRangeProcessing,
            LastModified,
            EntityTag);
    }
}

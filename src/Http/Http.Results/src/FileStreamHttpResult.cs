// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from a stream to the response.
/// </summary>
public sealed class FileStreamHttpResult : IResult, IFileHttpResult
{
    /// <summary>
    /// Creates a new <see cref="FileStreamHttpResult"/> instance with
    /// the provided <paramref name="fileStream"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    internal FileStreamHttpResult(Stream fileStream, string? contentType)
    {
        if (fileStream == null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        FileStream = fileStream;
        if (fileStream.CanSeek)
        {
            FileLength = fileStream.Length;
        }
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
    /// Gets or sets the stream with the file that will be sent back as the response.
    /// </summary>
    public Stream FileStream { get; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        await using (FileStream)
        {
            await HttpResultsWriter.WriteResultAsFileAsync(httpContext,
               (context, range, rangeLength) => FileResultHelper.WriteFileAsync(context, FileStream, range, rangeLength),
               FileDownloadName,
               FileLength,
               ContentType,
               EnableRangeProcessing,
               LastModified,
               EntityTag);
        }
    }
}

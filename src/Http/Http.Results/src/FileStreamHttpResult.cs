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

    /// <inheritdoc/>
    public string ContentType { get; internal set; }

    /// <inheritdoc/>
    public string? FileDownloadName { get; internal set; }

    /// <inheritdoc/>
    public DateTimeOffset? LastModified { get; internal set; }

    /// <inheritdoc/>
    public EntityTagHeaderValue? EntityTag { get; internal init; }

    /// <inheritdoc/>
    public bool EnableRangeProcessing { get; internal init; }

    /// <inheritdoc/>
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
            await HttpResultsWriter.WriteResultAsFileAsync(
                httpContext,
                fileHttpResult: this,
                (context, range, rangeLength) => FileResultHelper.WriteFileAsync(context, FileStream, range, rangeLength));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from a stream to the response.
/// </summary>
public sealed class FileStreamHttpResult : IResult, IFileHttpResult, IContentTypeHttpResult
{
    /// <summary>
    /// Creates a new <see cref="FileStreamHttpResult"/> instance with
    /// the provided <paramref name="fileStream"/> and the
    /// provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    internal FileStreamHttpResult(Stream fileStream, string? contentType)
        : this(fileStream, contentType, fileDownloadName: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FileStreamHttpResult"/> instance with
    /// the provided <paramref name="fileStream"/>, the provided <paramref name="contentType"/>
    /// and the provided <paramref name="fileDownloadName"/>.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    internal FileStreamHttpResult(
        Stream fileStream,
        string? contentType,
        string? fileDownloadName)
        : this(fileStream, contentType, fileDownloadName, enableRangeProcessing: false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FileStreamHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    internal FileStreamHttpResult(
        Stream fileStream,
        string? contentType,
        string? fileDownloadName,
        bool enableRangeProcessing,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        FileStream = fileStream;
        if (fileStream.CanSeek)
        {
            FileLength = fileStream.Length;
        }

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
    /// Gets the stream with the file that will be sent back as the response.
    /// </summary>
    public Stream FileStream { get; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.FileStreamResult");

        await using (FileStream)
        {
            var (range, rangeLength, completed) = HttpResultsHelper.WriteResultAsFileCore(
                httpContext,
                logger,
                FileDownloadName,
                FileLength,
                ContentType,
                EnableRangeProcessing,
                LastModified,
                EntityTag);

            if (!completed)
            {
                await FileResultHelper.WriteFileAsync(httpContext, FileStream, range, rangeLength);
            }
        }
    }
}

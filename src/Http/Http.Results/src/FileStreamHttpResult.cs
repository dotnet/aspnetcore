// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from a stream to the response.
/// </summary>
public sealed class FileStreamHttpResult : IResult
{
    /// <summary>
    /// Creates a new <see cref="FileStreamHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    public FileStreamHttpResult(Stream fileStream)
        : this(fileStream, contentType: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FileStreamHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileStream">The stream with the file.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public FileStreamHttpResult(Stream fileStream, string? contentType)
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
    /// Gets or sets the stream with the file that will be sent back as the response.
    /// </summary>
    public Stream FileStream { get; }

    /// <summary>
    /// Gets or sets the file length information .
    /// </summary>
    public long? FileLength { get; }

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
    public DateTimeOffset? LastModified { get; init; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
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

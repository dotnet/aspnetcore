// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from the content to the response.
/// </summary>
public sealed partial class FileContentHttpResult : IResult
{
    /// <summary>
    /// Creates a new <see cref="FileContentHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileContents">The bytes that represent the file contents.</param>
    public FileContentHttpResult(ReadOnlyMemory<byte> fileContents)
        : this(fileContents, contentType: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FileContentHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="fileContents">The bytes that represent the file contents.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public FileContentHttpResult(ReadOnlyMemory<byte> fileContents, string? contentType)
    {
        FileContents = fileContents;
        FileLength = fileContents.Length;
        ContentType = contentType ?? "application/octet-stream";
    }

    /// <summary>
    /// Gets the file contents.
    /// </summary>
    public ReadOnlyMemory<byte> FileContents { get; }

    /// <summary>
    /// Gets the file length information .
    /// </summary>
    public long? FileLength { get; }

    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    public string ContentType { get; init; }

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
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.FileContentResult");

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
            FileResultHelper.WriteFileAsync(httpContext, FileContents, range, rangeLength);
    }
}

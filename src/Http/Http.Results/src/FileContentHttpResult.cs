// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from the content to the response.
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
    /// Gets or sets the file contents.
    /// </summary>
    public ReadOnlyMemory<byte> FileContents { get; internal init; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext) => HttpResultsWriter.WriteResultAsFileAsync(
        httpContext,
        fileHttpResult: this,
        (context, range, rangeLength) => FileResultHelper.WriteFileAsync(httpContext, FileContents, range, rangeLength));
}

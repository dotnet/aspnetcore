// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from the writer callback to the response.
/// </summary>
public sealed class PushStreamHttpResult : IResult
{
    private readonly Func<Stream, Task> _streamWriterCallback;

    /// <summary>
    /// Creates a new <see cref="PushStreamHttpResult"/> instance with
    /// the provided <paramref name="streamWriterCallback"/> and the provided <paramref name="contentType"/>.
    /// </summary>
    /// <param name="streamWriterCallback">The stream writer callback.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    public PushStreamHttpResult(Func<Stream, Task> streamWriterCallback, string? contentType)
        : this(streamWriterCallback, contentType, fileDownloadName: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="PushStreamHttpResult"/> instance with
    /// the provided <paramref name="streamWriterCallback"/>, the provided <paramref name="contentType"/>
    /// and the provided <paramref name="fileDownloadName"/>.
    /// </summary>
    /// <param name="streamWriterCallback">The stream writer callback.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    public PushStreamHttpResult(
        Func<Stream, Task> streamWriterCallback,
        string? contentType,
        string? fileDownloadName)
        : this(streamWriterCallback, contentType, fileDownloadName, enableRangeProcessing: false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="PushStreamHttpResult"/> instance with the provided values.
    /// </summary>
    /// <param name="streamWriterCallback">The stream writer callback.</param>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    public PushStreamHttpResult(
        Func<Stream, Task> streamWriterCallback,
        string? contentType,
        string? fileDownloadName,
        bool enableRangeProcessing,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
    {
        _streamWriterCallback = streamWriterCallback;
        ContentType = contentType ?? "application/octet-stream";
        FileDownloadName = fileDownloadName;
        EnableRangeProcessing = enableRangeProcessing;
        LastModified = lastModified;
        EntityTag = entityTag;
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

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext) =>
        HttpResultsWriter.WriteResultAsFileAsync(
            httpContext,
            FileDownloadName,
            FileLength,
            ContentType,
            EnableRangeProcessing,
            LastModified,
            EntityTag,
            (context, _, _) => _streamWriterCallback(context.Response.Body));
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// write a file from the writer callback to the response.
/// </summary>
public sealed class PushStreamHttpResult : IResult, IFileHttpResult
{
    private readonly Func<Stream, Task> _streamWriterCallback;

    internal PushStreamHttpResult(Func<Stream, Task> streamWriterCallback, string? contentType)
    {
        _streamWriterCallback = streamWriterCallback;
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

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext) => HttpResultsWriter.WriteResultAsFileAsync(
            httpContext,
            fileHttpResult: this,
            (context, _, _) => _streamWriterCallback(httpContext.Response.Body));
}

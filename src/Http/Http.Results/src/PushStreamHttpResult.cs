// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed class PushStreamHttpResult : IResult, IFileHttpResult
{
    private readonly Func<Stream, Task> _streamWriterCallback;

    internal PushStreamHttpResult(Func<Stream, Task> streamWriterCallback, string? contentType)
    {
        _streamWriterCallback = streamWriterCallback;
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

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return HttpResultsWriter.WriteResultAsFileAsync(httpContext,
            (context, _, _) => _streamWriterCallback(httpContext.Response.Body),
            FileDownloadName,
            FileLength,
            ContentType,
            EnableRangeProcessing,
            LastModified,
            EntityTag);
    }
}

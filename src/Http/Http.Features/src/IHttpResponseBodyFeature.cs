// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// An aggregate of the different ways to interact with the response body.
/// </summary>
public interface IHttpResponseBodyFeature
{
    /// <summary>
    /// The <see cref="System.IO.Stream"/> for writing the response body.
    /// </summary>
    Stream Stream { get; }

    /// <summary>
    /// A <see cref="PipeWriter"/> representing the response body, if any.
    /// </summary>
    PipeWriter Writer { get; }

    /// <summary>
    /// Opts out of write buffering for the response.
    /// </summary>
    void DisableBuffering();

    /// <summary>
    /// Starts the response by calling OnStarting() and making headers unmodifiable.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the requested file in the response body. A response may include multiple writes.
    /// </summary>
    /// <param name="path">The full disk path to the file.</param>
    /// <param name="offset">The offset in the file to start at.</param>
    /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to abort the transmission.</param>
    /// <returns></returns>
    Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush any remaining response headers, data, or trailers.
    /// This may throw if the response is in an invalid state such as a Content-Length mismatch.
    /// </summary>
    /// <returns></returns>
    Task CompleteAsync();
}

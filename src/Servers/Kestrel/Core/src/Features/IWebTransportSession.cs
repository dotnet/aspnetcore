// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Controls the session and streams of a WebTransport session.
/// </summary>
public interface IWebTransportSession
{
    /// <summary>
    /// The id of the WebTransport session.
    /// </summary>
    public long SessionId { get; }

    /// <summary>
    /// Abruptly close the WebTransport session and stop all the streams.
    /// </summary>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    public void Abort();

    /// <summary>
    /// Returns the next incoming stream in the order which Kestel received it. The stream can be either bidirectional or unidirectional.
    /// </summary>
    /// <exception cref="Exception">If this is not a valid WebTransport session or it fails to get a stream to accept.</exception>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>The unidirectional or bidirectional stream that is next in the queue.</returns>
    public ValueTask<Stream> AcceptStreamAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a new unidirectional output stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <returns>The unidirectional stream that was opened.</returns>
    public ValueTask<Stream> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken);
}

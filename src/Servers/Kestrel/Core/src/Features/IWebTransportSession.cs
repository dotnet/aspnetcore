// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

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
    /// <param name="errorCode">HTTP error code that corresponds to the reason for causing the abort.</param>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    public void Abort(int errorCode = (int)Http3ErrorCode.NoError);

    /// <summary>
    /// Returns the next incoming stream in the order which Kestel received it. The stream can be either bidirectional or unidirectional.
    /// </summary>
    /// <remarks>To use WebTransport, you must first enable the Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams AppContextSwitch</remarks>
    /// <exception cref="Exception">If this is not a valid WebTransport session or it fails to get a stream to accept.</exception>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>The unidirectional or bidirectional stream that is next in the queue.</returns>
    public ValueTask<WebTransportStream> AcceptStreamAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a new unidirectional output stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <returns>The unidirectional stream that was opened.</returns>
    public ValueTask<WebTransportStream> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken);
}

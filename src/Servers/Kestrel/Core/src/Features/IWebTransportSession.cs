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
    long SessionId { get; }

    /// <summary>
    /// Abruptly close the WebTransport session and stop all the streams.
    /// </summary>
    /// <param name="errorCode">HTTP error code that corresponds to the reason for causing the abort.</param>
    /// <remarks>Error codes are described here: https://www.rfc-editor.org/rfc/rfc9114.html#name-http-3-error-codes</remarks>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    void Abort(int errorCode = (int)Http3ErrorCode.NoError);

    /// <summary>
    /// Returns the next incoming stream in the order which Kestel received it. The stream can be either bidirectional or unidirectional.
    /// </summary>
    /// <remarks>To use WebTransport, you must first enable the Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams AppContextSwitch</remarks>
    /// <exception cref="ObjectDisposedException">If this WebTransport session is closing</exception>
    /// <exception cref="Exception">If a stream with this id is already open</exception>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>The unidirectional or bidirectional stream that is next in the queue.</returns>
    ValueTask<WebTransportStream?> AcceptStreamAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a new unidirectional output stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <returns>The unidirectional stream that was opened.</returns>
    ValueTask<WebTransportStream> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken);
}

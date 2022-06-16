// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Controls the session and streams of a WebTransport session
/// </summary>
public interface IWebTransportSession
{
    /// <summary>
    /// The id of the WebTransport session.
    /// </summary>
    public long SessionId { get; }

    /// <summary>
    /// Close the WebTransport session and stops all the streams.
    /// </summary>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    public void Abort();

    /// <summary>
    /// Tries to open a new bidirectional stream.
    /// </summary>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <returns>The bidirectional stream or null if the operation failed.</returns>
    public ValueTask<IHttp3Stream?> OpenBidirectionalStream();

    /// <summary>
    /// Tries to open a new unidirectional stream.
    /// </summary>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <returns>The unidirectional stream or null if the operation failed.</returns>
    public ValueTask<IHttp3Stream?> OpenUnidirectionalStream();
}

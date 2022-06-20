// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Controls the session and streams of a WebTransport session
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
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
    /// Returns the next incoming stream
    /// </summary>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <param name="cancellationToken">The cancelation token used to cancel the operation</param>
    /// <returns>The unidirectional or bidirectional stream that is next in the queue</returns>
    public ValueTask<WebTransportBaseStream?> AcceptStreamAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Tries to open a new unidirectional stream.
    /// </summary>
    /// <exception cref="Exception">If This is not a valid WebTransport session.</exception>
    /// <returns>The unidirectional stream or null if the operation failed.</returns>
    public ValueTask<Stream?> OpenUnidirectionalStreamAsync();
}

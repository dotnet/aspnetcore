// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Controls the session and streams of a WebTransport session.
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
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
    void Abort(int errorCode);

    /// <summary>
    /// Returns the next incoming stream in the order the server received it. The stream can be either bidirectional or unidirectional.
    /// </summary>
    /// <remarks>To use WebTransport, you must first enable the <c>Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams</c> AppContextSwitch</remarks>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>The unidirectional or bidirectional stream that is next in the queue, or <c>null</c> if the session has ended.</returns>
    ValueTask<ConnectionContext?> AcceptStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a new unidirectional output stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>The unidirectional stream that was opened.</returns>
    ValueTask<ConnectionContext?> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken = default);
}

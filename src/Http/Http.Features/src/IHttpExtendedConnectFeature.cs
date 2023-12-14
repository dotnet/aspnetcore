// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Used with protocols that require the Extended CONNECT handshake such as HTTP/2 WebSockets and WebTransport.
/// https://www.rfc-editor.org/rfc/rfc8441#section-4
/// </summary>
public interface IHttpExtendedConnectFeature
{
    /// <summary>
    /// Indicates if the current request is a Extended CONNECT request that can be transitioned to an opaque connection via AcceptAsync.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Protocol))]
    bool IsExtendedConnect { get; }

    /// <summary>
    /// The <c>:protocol</c> header included in the request.
    /// </summary>
    string? Protocol { get; }

    /// <summary>
    /// Send the response headers with a 200 status code and transition to opaque streaming.
    /// </summary>
    /// <returns>An opaque bidirectional stream.</returns>
    ValueTask<Stream> AcceptAsync();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to server websocket features.
/// </summary>
public interface IHttpWebSocketFeature
{
    /// <summary>
    /// Indicates if this is a WebSocket upgrade request.
    /// </summary>
    bool IsWebSocketRequest { get; }

    /// <summary>
    /// Attempts to upgrade the request to a <see cref="WebSocket"/>. Check <see cref="IsWebSocketRequest"/>
    /// before invoking this.
    /// </summary>
    /// <param name="context">The <see cref="WebSocketAcceptContext"/>.</param>
    /// <returns>A <see cref="WebSocket"/>.</returns>
    Task<WebSocket> AcceptAsync(WebSocketAcceptContext context);
}

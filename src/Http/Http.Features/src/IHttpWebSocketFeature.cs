// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
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
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extensions methods to accept WebSocket connections.
    /// </summary>
    public static class WebSocketManagerExtensions
    {
        /// <summary>
        /// Transitions the request to a WebSocket connection using the specified options from the <see cref="ExtendedWebSocketAcceptContext"/>.
        /// </summary>
        /// <param name="webSocketManager">The <see cref="WebSocketManager"/> to accept the WebSocket with.</param>
        /// <param name="acceptContext">Options to use when accepting the WebSocket.</param>
        /// <returns>A WebSocket for bi-directional communication.</returns>
        public static Task<WebSocket> AcceptWebSocketAsync(this WebSocketManager webSocketManager, ExtendedWebSocketAcceptContext acceptContext)
        {
            return webSocketManager.AcceptWebSocketAsync(acceptContext);
        }
    }
}

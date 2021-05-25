// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A context for negotiating a websocket upgrade.
    /// </summary>
    public class WebSocketAcceptContext
    {
        /// <summary>
        /// Gets or sets the subprotocol being negotiated.
        /// </summary>
        public virtual string? SubProtocol { get; set; }

        /// <summary>
        /// The interval to send pong frames. This is a heart-beat that keeps the connection alive.
        /// </summary>
        public virtual TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// Enables support for the 'permessage-deflate' WebSocket extension.<para />
        /// Be aware that enabling compression makes the application subject to CRIME/BREACH type attacks.
        /// It is strongly advised to turn off compression when sending data containing secrets by
        /// specifying <see cref="WebSocketMessageFlags.DisableCompression"/> when sending such messages.
        /// </summary>
        public bool DangerousEnableCompression { get; set; }

        /// <summary>
        /// Disables server context takeover when using compression.
        /// </summary>
        /// <remarks>
        /// This property does nothing when <see cref="DangerousEnableCompression"/> is false,
        /// or when the client does not use compression.
        /// </remarks>
        /// <value>
        /// false
        /// </value>
        public bool DisableServerContextTakeover { get; set; } = false;

        /// <summary>
        /// Sets the maximum base-2 logarithm of the LZ77 sliding window size that can be used for compression.
        /// </summary>
        /// <remarks>
        /// This property does nothing when <see cref="DangerousEnableCompression"/> is false,
        /// or when the client does not use compression.
        /// </remarks>
        /// <value>
        /// 15
        /// </value>
        public int ServerMaxWindowBits { get; set; } = 15;
    }
}

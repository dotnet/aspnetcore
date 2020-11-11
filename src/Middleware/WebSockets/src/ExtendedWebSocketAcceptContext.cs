// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.WebSockets
{
    /// <summary>
    /// Extends the <see cref="WebSocketAcceptContext"/> class with additional properties.
    /// </summary>
    public class ExtendedWebSocketAcceptContext : WebSocketAcceptContext
    {
        /// <inheritdoc />
        public override string? SubProtocol { get; set; }

        /// <summary>
        /// This property is obsolete and has no effect.
        /// </summary>
        [Obsolete("Setting this property has no effect. It will be removed in a future version.")]
        public int? ReceiveBufferSize { get; set; }

        /// <summary>
        /// The interval to send pong frames. This is a heart-beat that keeps the connection alive.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }
    }
}

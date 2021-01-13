// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Configuration options for the WebSocketMiddleware
    /// </summary>
    public class WebSocketOptions
    {
        public WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2);
            ReceiveBufferSize = 4 * 1024;
            AllowedOrigins = new List<string>();
        }

        /// <summary>
        /// Gets or sets the frequency at which to send Ping/Pong keep-alive control frames.
        /// The default is two minutes.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; }

        /// <summary>
        /// Gets or sets the size of the protocol buffer used to receive and parse frames.
        /// The default is 4kb.
        /// </summary>
        public int ReceiveBufferSize { get; set; }

        /// <summary>
        /// Set the Origin header values allowed for WebSocket requests to prevent Cross-Site WebSocket Hijacking.
        /// By default all Origins are allowed.
        /// </summary>
        public IList<string> AllowedOrigins { get; }
    }
}
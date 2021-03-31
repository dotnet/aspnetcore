// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

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
    }
}

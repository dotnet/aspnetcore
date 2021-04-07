// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

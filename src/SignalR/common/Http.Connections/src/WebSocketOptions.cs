// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class WebSocketOptions
    {
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets a delegate that will be called when a new WebSocket is established to select the value
        /// for the 'Sec-WebSocket-Protocol' response header. The delegate will be called with a list of the protocols provided
        /// by the client in the 'Sec-WebSocket-Protocol' request header.
        /// </summary>
        /// <remarks>
        /// See RFC 6455 section 1.3 for more details on the WebSocket handshake: https://tools.ietf.org/html/rfc6455#section-1.3
        /// </remarks>
        // WebSocketManager's list of sub protocols is an IList:
        // https://github.com/aspnet/HttpAbstractions/blob/a6bdb9b1ec6ed99978a508e71a7f131be7e4d9fb/src/Microsoft.AspNetCore.Http.Abstractions/WebSocketManager.cs#L23
        // Unfortunately, IList<T> does not implement IReadOnlyList<T> :(
        public Func<IList<string>, string> SubProtocolSelector { get; set; }
    }
}

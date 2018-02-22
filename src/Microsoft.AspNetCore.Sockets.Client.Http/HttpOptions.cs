// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Sockets.Client.Http
{
    public class HttpOptions
    {
        public HttpMessageHandler HttpMessageHandler { get; set; }
        public IReadOnlyCollection<KeyValuePair<string, string>> Headers { get; set; }
        public Func<string> AccessTokenFactory { get; set; }
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets a delegate that will be invoked with the <see cref="ClientWebSocketOptions"/> object used
        /// by the <see cref="WebSocketsTransport"/> to configure the WebSocket.
        /// </summary>
        /// <remarks>
        /// This delegate is invoked after headers from <see cref="Headers"/> and the access token from <see cref="AccessTokenFactory"/>
        /// has been applied.
        /// </remarks>
        public Action<ClientWebSocketOptions> WebSocketOptions { get; set; }
    }
}

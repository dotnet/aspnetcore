// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http
{
    public abstract class WebSocketManager
    {
        public abstract bool IsWebSocketRequest { get; }

        public abstract IList<string> WebSocketRequestedProtocols { get; }

        public virtual Task<WebSocket> AcceptWebSocketAsync()
        {
            return AcceptWebSocketAsync(subProtocol: null);
        }

        public abstract Task<WebSocket> AcceptWebSocketAsync(string subProtocol);
    }
}

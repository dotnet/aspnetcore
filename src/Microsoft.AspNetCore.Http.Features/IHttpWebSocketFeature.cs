// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Features
{
    public interface IHttpWebSocketFeature
    {
        bool IsWebSocketRequest { get; }

        Task<WebSocket> AcceptAsync(WebSocketAcceptContext context);
    }
}
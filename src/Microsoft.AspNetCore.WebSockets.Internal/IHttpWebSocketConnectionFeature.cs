// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.WebSockets.Internal;

namespace Microsoft.AspNetCore.WebSockets.Internal
{
    public interface IHttpWebSocketConnectionFeature
    {
        bool IsWebSocketRequest { get; }
        ValueTask<IWebSocketConnection> AcceptWebSocketConnectionAsync(WebSocketAcceptContext context);
    }
}
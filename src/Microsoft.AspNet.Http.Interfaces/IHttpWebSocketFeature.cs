// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http
{
    public interface IHttpWebSocketFeature
    {
        bool IsWebSocketRequest { get; }

        Task<WebSocket> AcceptAsync(IWebSocketAcceptContext context);
    }
}
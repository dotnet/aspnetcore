// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Interfaces;

namespace Microsoft.AspNet.WebSockets.Server
{
    public class WebSocketAcceptContext : IWebSocketAcceptContext
    {
        public string SubProtocol { get; set; }
        public int? ReceiveBufferSize { get; set; }
        public TimeSpan? KeepAliveInterval { get; set; }

        // public ArraySegment<byte>? Buffer { get; set; } // TODO
    }
}
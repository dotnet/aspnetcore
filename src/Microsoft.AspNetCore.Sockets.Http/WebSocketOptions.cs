// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Sockets
{
    public class WebSocketOptions
    {
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public WebSocketMessageType WebSocketMessageType { get; set; } = WebSocketMessageType.Text;
    }
}

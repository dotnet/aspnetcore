// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Sockets
{
    public class HttpSocketOptions
    {
        public IList<string> AuthorizationPolicyNames { get; } = new List<string>();

        public TransportType Transports { get; set; } = TransportType.All;

        public WebSocketOptions WebSockets { get; } = new WebSocketOptions();
    }
}
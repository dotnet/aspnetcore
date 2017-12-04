// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Sockets
{
    public class HttpSocketOptions
    {
        public IList<IAuthorizeData> AuthorizationData { get; } = new List<IAuthorizeData>();

        public TransportType Transports { get; set; } = TransportType.All;

        public WebSocketOptions WebSockets { get; } = new WebSocketOptions();

        public LongPollingOptions LongPolling { get; } = new LongPollingOptions();
    }
}

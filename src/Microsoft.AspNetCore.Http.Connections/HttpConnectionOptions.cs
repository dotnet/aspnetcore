// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Internal;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class HttpConnectionOptions
    {
        public HttpConnectionOptions()
        {
            AuthorizationData = new List<IAuthorizeData>();
            Transports = HttpTransports.All;
            WebSockets = new WebSocketOptions();
            LongPolling = new LongPollingOptions();
            TransportMaxBufferSize = 0;
            ApplicationMaxBufferSize = 0;
        }

        public IList<IAuthorizeData> AuthorizationData { get; }

        public HttpTransportType Transports { get; set; }

        public WebSocketOptions WebSockets { get; }

        public LongPollingOptions LongPolling { get; }

        public long TransportMaxBufferSize { get; set; }

        public long ApplicationMaxBufferSize { get; set; }
    }
}

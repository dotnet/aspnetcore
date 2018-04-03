// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class HttpConnectionOptions
    {
        public IList<IAuthorizeData> AuthorizationData { get; } = new List<IAuthorizeData>();

        public HttpTransportType Transports { get; set; } = HttpTransportType.All;

        public WebSocketOptions WebSockets { get; } = new WebSocketOptions();

        public LongPollingOptions LongPolling { get; } = new LongPollingOptions();

        public long TransportMaxBufferSize { get; set; } = 0;

        public long ApplicationMaxBufferSize { get; set; } = 0;
    }
}

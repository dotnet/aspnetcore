// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Internal;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class HttpConnectionDispatcherOptions
    {
        // Selected because this is the default value of PipeWriter.PauseWriterThreshold.
        // There maybe the opportunity for performance gains by tuning this default.
        private const int DefaultPipeBufferSize = 32768;

        public HttpConnectionDispatcherOptions()
        {
            AuthorizationData = new List<IAuthorizeData>();
            Transports = HttpTransports.All;
            WebSockets = new WebSocketOptions();
            LongPolling = new LongPollingOptions();
            TransportMaxBufferSize = DefaultPipeBufferSize;
            ApplicationMaxBufferSize = DefaultPipeBufferSize;
        }

        public IList<IAuthorizeData> AuthorizationData { get; }

        public HttpTransportType Transports { get; set; }

        public WebSocketOptions WebSockets { get; }

        public LongPollingOptions LongPolling { get; }

        public long TransportMaxBufferSize { get; set; }

        public long ApplicationMaxBufferSize { get; set; }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Options used to configure the HTTP connection dispatcher.
    /// </summary>
    public class HttpConnectionDispatcherOptions
    {
        // Selected because this is the default value of PipeWriter.PauseWriterThreshold.
        // There maybe the opportunity for performance gains by tuning this default.
        private const int DefaultPipeBufferSize = 32768;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnectionDispatcherOptions"/> class.
        /// </summary>
        public HttpConnectionDispatcherOptions()
        {
            AuthorizationData = new List<IAuthorizeData>();
            Transports = HttpTransports.All;
            WebSockets = new WebSocketOptions();
            LongPolling = new LongPollingOptions();
            TransportMaxBufferSize = DefaultPipeBufferSize;
            ApplicationMaxBufferSize = DefaultPipeBufferSize;
        }

        /// <summary>
        /// Gets a collection of <see cref="IAuthorizeData"/> used during HTTP connection pipeline.
        /// </summary>
        public IList<IAuthorizeData> AuthorizationData { get; }

        /// <summary>
        /// Gets or sets a bitmask combining one or more <see cref="HttpTransportType"/> values that specify what transports the server should use to receive HTTP requests.
        /// </summary>
        public HttpTransportType Transports { get; set; }

        /// <summary>
        /// Gets the <see cref="WebSocketOptions"/> used by the web sockets transport.
        /// </summary>
        public WebSocketOptions WebSockets { get; }

        /// <summary>
        /// Gets the <see cref="LongPollingOptions"/> used by the long polling transport.
        /// </summary>
        public LongPollingOptions LongPolling { get; }

        /// <summary>
        /// Gets or sets the maximum buffer size of the transport writer.
        /// </summary>
        public long TransportMaxBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum buffer size of the application writer.
        /// </summary>
        public long ApplicationMaxBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum protocol verison supported by the server.
        /// The default value is 0, the lowest possible protocol version.
        /// </summary>
        public int MinimumProtocolVersion { get; set; } = 0;
    }
}

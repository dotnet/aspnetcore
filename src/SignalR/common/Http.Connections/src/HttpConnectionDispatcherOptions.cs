// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Pipelines;
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

        private PipeOptions? _transportPipeOptions;
        private PipeOptions? _appPipeOptions;

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

        // We initialize these lazily based on the state of the options specified here.
        // Though these are mutable it's extremely rare that they would be mutated past the
        // call to initialize the routerware.
        internal PipeOptions TransportPipeOptions => _transportPipeOptions ??= new PipeOptions(pauseWriterThreshold: TransportMaxBufferSize, resumeWriterThreshold: TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);

        internal PipeOptions AppPipeOptions => _appPipeOptions ??= new PipeOptions(pauseWriterThreshold: ApplicationMaxBufferSize, resumeWriterThreshold: ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class LibuvTransportFactory : IConnectionListenerFactory
    {
        private readonly LibuvTransportContext _baseTransportContext;

        public LibuvTransportFactory(
            IOptions<LibuvTransportOptions> options,
            IHostApplicationLifetime applicationLifetime,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv");
            var trace = new LibuvTrace(logger);

            var threadCount = options.Value.ThreadCount;

            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCount),
                    threadCount,
                    "ThreadCount must be positive.");
            }

            if (!LibuvConstants.ECONNRESET.HasValue)
            {
                trace.LogWarning("Unable to determine ECONNRESET value on this platform.");
            }

            if (!LibuvConstants.EADDRINUSE.HasValue)
            {
                trace.LogWarning("Unable to determine EADDRINUSE value on this platform.");
            }

            _baseTransportContext = new LibuvTransportContext
            {
                Options = options.Value,
                AppLifetime = applicationLifetime,
                Log = trace,
            };
        }

        public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var transportContext = new LibuvTransportContext
            {
                Options = _baseTransportContext.Options,
                AppLifetime = _baseTransportContext.AppLifetime,
                Log = _baseTransportContext.Log
            };

            var transport = new LibuvConnectionListener(transportContext, endpoint);
            await transport.BindAsync();
            return transport;
        }
    }
}

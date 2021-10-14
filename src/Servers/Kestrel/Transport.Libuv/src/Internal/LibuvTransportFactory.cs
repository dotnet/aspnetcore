// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#pragma warning disable CS0618
            IOptions<LibuvTransportOptions> options,
#pragma warning restore CS0618
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

#pragma warning disable CS0618
            var threadCount = options.Value.ThreadCount;
#pragma warning restore CS0618

            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCount),
                    threadCount,
                    "ThreadCount must be positive.");
            }

            if (!LibuvConstants.ECONNRESET.HasValue)
            {
                logger.LogWarning("Unable to determine ECONNRESET value on this platform.");
            }

            if (!LibuvConstants.EADDRINUSE.HasValue)
            {
                logger.LogWarning("Unable to determine EADDRINUSE value on this platform.");
            }

            _baseTransportContext = new LibuvTransportContext
            {
                Options = options.Value,
                AppLifetime = applicationLifetime,
                Log = logger,
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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv
{
    public class LibuvTransportFactory : ITransportFactory
    {
        private readonly LibuvTransportContext _baseTransportContext;

        public LibuvTransportFactory(
            IOptions<LibuvTransportOptions> options,
            IApplicationLifetime applicationLifetime,
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

            // REVIEW: Should we change the logger namespace for transport logs?
            var logger  = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel");
            // TODO: Add LibuvTrace
            var trace = new KestrelTrace(logger);

            var threadCount = options.Value.ThreadCount;

            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCount),
                    threadCount,
                    "ThreadCount must be positive.");
            }

            if (!Constants.ECONNRESET.HasValue)
            {
                trace.LogWarning("Unable to determine ECONNRESET value on this platform.");
            }

            if (!Constants.EADDRINUSE.HasValue)
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

        public ITransport Create(ListenOptions listenOptions, IConnectionHandler handler)
        {
            var transportContext = new LibuvTransportContext
            {
                Options = _baseTransportContext.Options,
                AppLifetime = _baseTransportContext.AppLifetime,
                Log = _baseTransportContext.Log,
                ConnectionHandler = handler
            };

            return new KestrelEngine(transportContext, listenOptions);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
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

            var logger  = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv");
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

        public ITransport Create(IEndPointInformation endPointInformation, IConnectionDispatcher dispatcher)
        {
            var transportContext = new LibuvTransportContext
            {
                Options = _baseTransportContext.Options,
                AppLifetime = _baseTransportContext.AppLifetime,
                Log = _baseTransportContext.Log,
                ConnectionDispatcher = dispatcher
            };

            return new LibuvTransport(transportContext, endPointInformation);
        }
    }
}

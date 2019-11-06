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

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicTransportFactory : IConnectionListenerFactory
    {
        private readonly MsQuicTransportContext _transportContext;

        public MsQuicTransportFactory(IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<MsQuicTransportOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic");
            var trace = new MsQuicTrace(logger);

            _transportContext = new MsQuicTransportContext(applicationLifetime, trace, options.Value);
        }

        public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var transport = new MsQuicConnectionListener(_transportContext, endpoint);
            await transport.BindAsync();
            return transport;
        }
    }
}

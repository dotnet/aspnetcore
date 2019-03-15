// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
#pragma warning disable PUB0001 // Pubternal type in public API
    public sealed class SocketTransportFactory : ITransportFactory
#pragma warning restore PUB0001 // Pubternal type in public API
    {
        private readonly SocketTransportOptions _options;
        private readonly IApplicationLifetime _appLifetime;
        private readonly SocketsTrace _trace;

        public SocketTransportFactory(
            IOptions<SocketTransportOptions> options,
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

            _options = options.Value;
            _appLifetime = applicationLifetime;
            var logger  = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            _trace = new SocketsTrace(logger);
        }

#pragma warning disable PUB0001 // Pubternal type in public API
        public ITransport Create(IEndPointInformation endPointInformation, IConnectionDispatcher dispatcher)
#pragma warning restore PUB0001 // Pubternal type in public API
        {
            if (endPointInformation == null)
            {
                throw new ArgumentNullException(nameof(endPointInformation));
            }

            if (endPointInformation.Type != ListenType.IPEndPoint)
            {
                throw new ArgumentException(SocketsStrings.OnlyIPEndPointsSupported, nameof(endPointInformation));
            }

            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            return new SocketTransport(endPointInformation, dispatcher, _appLifetime, _options.IOQueueCount, _trace, _options.MemoryPoolFactory());
        }
    }
}

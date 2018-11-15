// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    public class LoggingConnectionAdapter : IConnectionAdapter
    {
        private readonly ILogger _logger;

        public LoggingConnectionAdapter(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        public bool IsHttps => false;

        public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
        {
            return Task.FromResult<IAdaptedConnection>(
                new LoggingAdaptedConnection(context.ConnectionStream, _logger));
        }

        private class LoggingAdaptedConnection : IAdaptedConnection
        {
            public LoggingAdaptedConnection(Stream rawStream, ILogger logger)
            {
                ConnectionStream = new LoggingStream(rawStream, logger);
            }

            public Stream ConnectionStream { get; }

            public void Dispose()
            {
            }
        }
    }
}

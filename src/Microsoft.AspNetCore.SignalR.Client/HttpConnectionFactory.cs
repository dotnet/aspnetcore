// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HttpConnectionFactory : IConnectionFactory
    {
        private readonly HttpConnectionOptions _httpConnectionOptions;
        private readonly ILoggerFactory _loggerFactory;

        public HttpConnectionFactory(IOptions<HttpConnectionOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _httpConnectionOptions = options.Value;
            _loggerFactory = loggerFactory;
        }

        public async Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat, CancellationToken cancellationToken = default)
        {
            var connection = new HttpConnection(_httpConnectionOptions, _loggerFactory);
            try
            {
                await connection.StartAsync(transferFormat, cancellationToken);
                return connection;
            }
            catch
            {
                // Make sure the connection is disposed, in case it allocated any resources before failing.
                await connection.DisposeAsync();
                throw;
            }
        }

        public Task DisposeAsync(ConnectionContext connection)
        {
            return ((HttpConnection)connection).DisposeAsync();
        }
    }
}

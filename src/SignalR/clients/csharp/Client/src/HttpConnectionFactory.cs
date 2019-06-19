// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// A factory for creating <see cref="HttpConnection"/> instances.
    /// </summary>
    public class HttpConnectionFactory : IConnectionFactory
    {
        private readonly TransferFormat _transferFormat; 
        private readonly HttpConnectionOptions _httpConnectionOptions;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnectionFactory"/> class.
        /// </summary>
        /// <param name="hubProtocol">The <see cref="IHubProtocol"/> that will use the connections.</param>
        /// <param name="options">The connection options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public HttpConnectionFactory(IHubProtocol hubProtocol, IOptions<HttpConnectionOptions> options, ILoggerFactory loggerFactory)
        {
            if (hubProtocol == null)
            {
                throw new ArgumentNullException(nameof(hubProtocol));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _transferFormat = hubProtocol.TransferFormat;
            _httpConnectionOptions = options.Value;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public async Task<ConnectionContext> ConnectAsync(EndPoint httpEndPoint, CancellationToken cancellationToken = default)
        {
            var connection = new HttpConnection(_httpConnectionOptions, _loggerFactory);

            try
            {
                await connection.StartAsync(cancellationToken);
                return connection;
            }
            catch
            {
                // Make sure the connection is disposed, in case it allocated any resources before failing.
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}

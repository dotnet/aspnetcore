// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HttpConnectionFactory : IConnectionFactory
    {
        private readonly HttpConnectionOptions _options;
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
            
            _options = options.Value;
            _loggerFactory = loggerFactory;
        }

        public async Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat)
        {
            var httpOptions = new HttpOptions
            {
                HttpMessageHandlerFactory = _options.MessageHandlerFactory,
                Headers = _options._headers != null ? new ReadOnlyDictionary<string, string>(_options._headers) : null,
                AccessTokenFactory = _options.AccessTokenFactory,
                WebSocketOptions = _options.WebSocketOptions,
                Cookies = _options._cookies,
                Proxy = _options.Proxy,
                UseDefaultCredentials = _options.UseDefaultCredentials,
                ClientCertificates = _options._clientCertificates,
                Credentials = _options.Credentials,
            };

            var connection = new HttpConnection(_options.Url, _options.Transports, _loggerFactory, httpOptions);
            await connection.StartAsync(transferFormat);
            return connection;
        }
    }
}

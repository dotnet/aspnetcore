// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        private static HttpConnection CreateConnection(
            HttpMessageHandler httpHandler = null,
            ILoggerFactory loggerFactory = null,
            string url = null,
            ITransport transport = null,
            ITransportFactory transportFactory = null,
            HttpTransportType? transportType = null,
            Func<Task<string>> accessTokenProvider = null)
        {
            var httpOptions = new HttpConnectionOptions
            {
                Transports = transportType ?? HttpTransportType.LongPolling,
                HttpMessageHandlerFactory = (httpMessageHandler) => httpHandler ?? TestHttpMessageHandler.CreateDefault(),
                AccessTokenProvider = accessTokenProvider,
            };
            if (url != null)
            {
                httpOptions.Url = new Uri(url);
            }

            return CreateConnection(httpOptions, loggerFactory, transport, transportFactory);
        }

        private static HttpConnection CreateConnection(HttpConnectionOptions httpConnectionOptions, ILoggerFactory loggerFactory = null, ITransport transport = null, ITransportFactory transportFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            httpConnectionOptions.Url = httpConnectionOptions.Url ?? new Uri("http://fakeuri.org/");

            if (transportFactory != null)
            {
                return new HttpConnection(httpConnectionOptions, loggerFactory, transportFactory);
            }
            else if (transport != null)
            {
                return new HttpConnection(httpConnectionOptions, loggerFactory, new TestTransportFactory(transport));
            }
            else
            {
                return new HttpConnection(httpConnectionOptions, loggerFactory);
            }
        }

        private static async Task WithConnectionAsync(HttpConnection connection, Func<HttpConnection, Task> body)
        {
            try
            {
                // Using OrTimeout here will hide any timeout issues in the test :(.
                await body(connection);
            }
            finally
            {
                await connection.DisposeAsync().OrTimeout();
            }
        }
    }
}


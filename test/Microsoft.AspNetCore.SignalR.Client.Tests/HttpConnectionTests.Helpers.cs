// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        private static HttpConnection CreateConnection(HttpMessageHandler httpHandler = null, ILoggerFactory loggerFactory = null, string url = null, ITransport transport = null, ITransportFactory transportFactory = null)
        {
            var httpOptions = new HttpOptions()
            {
                HttpMessageHandler = (httpMessageHandler) => httpHandler ?? TestHttpMessageHandler.CreateDefault(),
            };

            return CreateConnection(httpOptions, loggerFactory, url, transport, transportFactory);
        }

        private static HttpConnection CreateConnection(HttpOptions httpOptions, ILoggerFactory loggerFactory = null, string url = null, ITransport transport = null, ITransportFactory transportFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            var uri = new Uri(url ?? "http://fakeuri.org/");

            if (transportFactory != null)
            {
                return new HttpConnection(uri, transportFactory, loggerFactory, httpOptions);
            }
            else if (transport != null)
            {
                return new HttpConnection(uri, new TestTransportFactory(transport), loggerFactory, httpOptions);
            }
            else
            {
                return new HttpConnection(uri, TransportType.LongPolling, loggerFactory, httpOptions);
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


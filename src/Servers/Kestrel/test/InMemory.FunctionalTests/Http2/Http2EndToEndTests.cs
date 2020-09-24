// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.Http2
{
    public class Http2EndToEndTests : TestApplicationErrorLoggerLoggedTest
    {
        [Fact]
        public async Task MiddlewareIsRunWithConnectionLoggingScopeForHttp2Requests()
        {
            await using var server = new TestServer(async context =>
            {
                await context.Response.WriteAsync("hello, world");
            },
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });

            var connectionCount = 0;
            using var connection = server.CreateConnection();

            using var socketsHandler = new SocketsHttpHandler()
            {
                ConnectCallback = (_, _) =>
                {
                    if (connectionCount != 0)
                    {
                        throw new InvalidOperationException();
                    }

                    connectionCount++;
                    return new ValueTask<Stream>(connection.Stream);
                },
            };

            using var httpClient = new HttpClient(socketsHandler);

            using var httpRequsetMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri($"http://localhost:{server.Port}/"),
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };

            using var responseMessage = await httpClient.SendAsync(httpRequsetMessage);

            Assert.Equal("hello, world", await responseMessage.Content.ReadAsStringAsync());
        }
    }
}

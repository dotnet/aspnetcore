// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.TestHost.Tests
{
    public class WebSocketClientTests
    {
        [Fact]
        public async Task ConnectAsync_ShouldSetRequestProperties()
        {
            HttpRequest capturedRequest = null;

            using (var testServer = new TestServer(new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(async (ctx, next) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/connect"))
                        {
                            capturedRequest = ctx.Request;
                            return;
                        }

                        await next();
                    });
                })))
            {
                var client = testServer.CreateWebSocketClient();

                try
                {
                    await client.ConnectAsync(
                        uri: new Uri(testServer.BaseAddress, "/connect"),
                        cancellationToken: default(CancellationToken));
                }
                catch
                {
                    // An exception will be thrown because our dummy endpoint does not implement a full Web socket server
                }
            }

            Assert.Equal("http", capturedRequest.Scheme);
            Assert.Equal("localhost", capturedRequest.Host.Value);
            Assert.Equal("/connect", capturedRequest.Path);
        }
    }
}

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
        [Theory]
        [InlineData("http://localhost/connect", "localhost")]
        [InlineData("http://localhost:80/connect", "localhost")]
        [InlineData("http://localhost:81/connect", "localhost:81")]
        public async Task ConnectAsync_ShouldSetRequestProperties(string requestUri, string expectedHost)
        {
            HttpRequest capturedRequest = null;

            using (var testServer = new TestServer(new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run(ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/connect"))
                        {
                            capturedRequest = ctx.Request;
                        }
                        return Task.FromResult(0);
                    });
                })))
            {
                var client = testServer.CreateWebSocketClient();

                try
                {
                    await client.ConnectAsync(
                        uri: new Uri(requestUri),
                        cancellationToken: default(CancellationToken));
                }
                catch
                {
                    // An exception will be thrown because our dummy endpoint does not implement a full Web socket server
                }
            }

            Assert.Equal("http", capturedRequest.Scheme);
            Assert.Equal(expectedHost, capturedRequest.Host.Value);
            Assert.Equal("/connect", capturedRequest.Path);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets.Tests;

public class Http2WebSocketTests
{
    [Fact]
    public async Task Http2Handshake_Success()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.Configure(app =>
                {
                    app.UseWebSockets();
                    app.Run(httpContext =>
                    {
                        Assert.True(httpContext.WebSockets.IsWebSocketRequest);
                        Assert.Equal(new[] { "p1", "p2" }, httpContext.WebSockets.WebSocketRequestedProtocols);
                        return httpContext.WebSockets.AcceptWebSocketAsync("p2");
                    });
                });
            }).Start();

        var testServer = host.GetTestServer();

        var result = await testServer.SendAsync(httpContext =>
        {
            httpContext.Request.Method = HttpMethods.Connect;
            httpContext.Features.Set<IHttpExtendedConnectFeature>(new ConnectFeature()
            {
                IsExtendedConnect = true,
                Protocol = "WebSocket",
            });
            httpContext.Request.Headers.SecWebSocketVersion = Constants.Headers.SupportedVersion;
            httpContext.Request.Headers.SecWebSocketProtocol = "p1, p2";
        });

        Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        var headers = result.Response.Headers;
        Assert.Equal("p2", headers.SecWebSocketProtocol);
        Assert.False(headers.TryGetValue(HeaderNames.Connection, out var _));
        Assert.False(headers.TryGetValue(HeaderNames.Upgrade, out var _));
        Assert.False(headers.TryGetValue(HeaderNames.SecWebSocketAccept, out var _));
    }

    public sealed class ConnectFeature : IHttpExtendedConnectFeature
    {
        public bool IsExtendedConnect { get; set; }
        public string Protocol { get; set; }
        public Stream Stream { get; set; } = Stream.Null;

        /// <inheritdoc/>
        public ValueTask<Stream> AcceptAsync()
        {
            if (!IsExtendedConnect)
            {
                throw new InvalidOperationException("This is not an Extended CONNECT request.");
            }

            return new ValueTask<Stream>(Stream);
        }
    }
}

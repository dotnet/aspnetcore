// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost.Tests;

public class WebSocketClientTests
{
    [Theory]
    [InlineData("http://localhost/connect", "localhost")]
    [InlineData("http://localhost:80/connect", "localhost")]
    [InlineData("http://localhost:81/connect", "localhost:81")]
    public async Task ConnectAsync_ShouldSetRequestProperties(string requestUri, string expectedHost)
    {
        string capturedScheme = null;
        string capturedHost = null;
        string capturedPath = null;

        using (var testServer = new TestServer(new WebHostBuilder()
            .Configure(app =>
            {
                app.Run(ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/connect"))
                    {
                        capturedScheme = ctx.Request.Scheme;
                        capturedHost = ctx.Request.Host.Value;
                        capturedPath = ctx.Request.Path;
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
                    cancellationToken: default);
            }
            catch
            {
                // An exception will be thrown because our dummy endpoint does not implement a full Web socket server
            }
        }

        Assert.Equal("http", capturedScheme);
        Assert.Equal(expectedHost, capturedHost);
        Assert.Equal("/connect", capturedPath);
    }

    [Fact]
    public async Task CanAcceptWebSocket()
    {
        using (var testServer = new TestServer(new WebHostBuilder()
            .Configure(app =>
            {
                app.UseWebSockets();
                app.Run(async ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/connect"))
                    {
                        if (ctx.WebSockets.IsWebSocketRequest)
                        {
                            using var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                            var buffer = new byte[1000];
                            var res = await websocket.ReceiveAsync(buffer, default);
                            await websocket.SendAsync(buffer.AsMemory(0, res.Count), System.Net.WebSockets.WebSocketMessageType.Binary, true, default);
                            await websocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, null, default);
                        }
                    }
                });
            })))
        {
            var client = testServer.CreateWebSocketClient();

            using var socket = await client.ConnectAsync(
                uri: new Uri("http://localhost/connect"),
                cancellationToken: default);

            await socket.SendAsync(new byte[10], System.Net.WebSockets.WebSocketMessageType.Binary, true, default);
            var res = await socket.ReceiveAsync(new byte[100], default);
            Assert.Equal(10, res.Count);
            Assert.True(res.EndOfMessage);

            await socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, null, default);
        }
    }

    [Fact]
    public async Task VerifyWebSocketAndUpgradeFeatures()
    {
        using (var testServer = new TestServer(new WebHostBuilder()
            .Configure(app =>
            {
                app.Run(async c =>
                {
                    var upgradeFeature = c.Features.Get<IHttpUpgradeFeature>();
                    Assert.NotNull(upgradeFeature);
                    Assert.False(upgradeFeature.IsUpgradableRequest);
                    await Assert.ThrowsAsync<NotSupportedException>(() => upgradeFeature.UpgradeAsync());

                    var webSocketFeature = c.Features.Get<IHttpWebSocketFeature>();
                    Assert.NotNull(webSocketFeature);
                    Assert.True(webSocketFeature.IsWebSocketRequest);
                });
            })))
        {
            var client = testServer.CreateWebSocketClient();

            try
            {
                using var socket = await client.ConnectAsync(
                    uri: new Uri("http://localhost/connect"),
                    cancellationToken: default);
            }
            catch
            {
                // An exception will be thrown because our endpoint does not accept the websocket
            }
        }
    }
}

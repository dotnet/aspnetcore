// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace TestSite;

public partial class Startup
{
    private void WebSocketNotUpgradable(IApplicationBuilder app)
    {
        app.Run(context =>
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            Assert.False(upgradeFeature.IsUpgradableRequest);
            return Task.CompletedTask;
        });
    }

    private void WebSocketUpgradable(IApplicationBuilder app)
    {
        app.Run(context =>
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            Assert.True(upgradeFeature.IsUpgradableRequest);
            return Task.CompletedTask;
        });
    }

    private void WebSocketReadBeforeUpgrade(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var singleByteArray = new byte[1];
            Assert.Equal(0, await context.Request.Body.ReadAsync(singleByteArray, 0, 1));

            var ws = await Upgrade(context);
            await SendMessages(ws, "Yay");
        });
    }

    private void WebSocketEcho(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var ws = await Upgrade(context);
#if FORWARDCOMPAT
            var appLifetime = app.ApplicationServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IApplicationLifetime>();
#else
            var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
#endif

            await Echo(ws, appLifetime.ApplicationStopping);
        });
    }

    private void WebSocketLifetimeEvents(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var messages = new List<string>();

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["custom-header"] = "value";
                messages.Add("OnStarting");
                return Task.CompletedTask;
            });

            var ws = await Upgrade(context);
            messages.Add("Upgraded");

            await SendMessages(ws, messages.ToArray());
        });
    }

    private void WebSocketUpgradeFails(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => upgradeFeature.UpgradeAsync());
            Assert.Equal("Upgrade requires HTTP/1.1.", ex.Message);
        });
    }

    private static async Task SendMessages(WebSocket webSocket, params string[] messages)
    {
        foreach (var message in messages)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private static async Task<WebSocket> Upgrade(HttpContext context)
    {
        var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

        // Generate WebSocket response headers
        string key = context.Request.Headers[Constants.Headers.SecWebSocketKey].ToString();
        var responseHeaders = HandshakeHelpers.GenerateResponseHeaders(key);
        foreach (var headerPair in responseHeaders)
        {
            context.Response.Headers[headerPair.Key] = headerPair.Value;
        }

        // Upgrade the connection
        Stream opaqueTransport = await upgradeFeature.UpgradeAsync();
        Assert.Null(context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize);

        // Get the WebSocket object
        var ws = WebSocket.CreateFromStream(opaqueTransport, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromMinutes(2));
        return ws;
    }

    private async Task Echo(WebSocket webSocket, CancellationToken token)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
        bool closeFromServer = false;
        string closeFromServerCmd = "CloseFromServer";
        int closeFromServerLength = closeFromServerCmd.Length;

        while (!result.CloseStatus.HasValue && !token.IsCancellationRequested && !closeFromServer)
        {
            if (result.Count == closeFromServerLength &&
                Encoding.ASCII.GetString(buffer).Substring(0, result.Count) == closeFromServerCmd)
            {
                // The client sent "CloseFromServer" text message to request the server to close (a test scenario).
                closeFromServer = true;
            }
            else
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, token);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            }
        }

        if (result.CloseStatus.HasValue)
        {
            // Client-initiated close handshake
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        else
        {
            // Server-initiated close handshake due to either of the two conditions:
            // (1) The applicaton host is performing a graceful shutdown.
            // (2) The client sent "CloseFromServer" text message to request the server to close (a test scenario).
            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeFromServerCmd, CancellationToken.None);

            // The server has sent the Close frame.
            // Stop sending but keep receiving until we get the Close frame from the client.
            while (!result.CloseStatus.HasValue)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
    }
}

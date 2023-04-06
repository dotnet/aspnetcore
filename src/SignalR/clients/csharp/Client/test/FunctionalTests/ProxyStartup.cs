// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public class ProxyStartup
{
    private string ServerUrl;

    public void ConfigureServices(IServiceCollection services)
    {
        // Since tests run in parallel, it's possible multiple servers will startup and read files being written by another test
        // Use a unique directory per server to avoid this collision
        services.AddDataProtection()
            .PersistKeysToFileSystem(Directory.CreateDirectory(Path.GetRandomFileName()));

        services.AddWebSockets(o => o.KeepAliveInterval = TimeSpan.Zero);

        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseWebSockets();

        app.Use(next =>
        {
            return async context =>
            {
                if (context.Request.Path.Value.EndsWith("/server", StringComparison.Ordinal))
                {
                    ServerUrl = context.Request.Query["url"];
                }
                else if (context.Request.Path.Value.EndsWith("/drop", StringComparison.Ordinal))
                {
                    // TODO: drop connection
                    // for testing seamless reconnect
                }
                else
                {
                    // TODO: forward to server
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var uriBuilder = new UriBuilder(ServerUrl);
                        uriBuilder.Path = context.Request.Path;
                        uriBuilder.Scheme = context.Request.IsHttps ? "wss" : "ws";
                        uriBuilder.Query = context.Request.QueryString.Value;
                        using var ws = await context.WebSockets.AcceptWebSocketAsync();
                        using var forwardingWebsocket = new ClientWebSocket();
                        await forwardingWebsocket.ConnectAsync(uriBuilder.Uri, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);
                        var recvTask = Forward(ws, forwardingWebsocket);
                        var sendTask = Forward(forwardingWebsocket, ws);

                        await Task.WhenAny(recvTask, sendTask);
                    }
                    else
                    {
                        var uriBuilder = new UriBuilder(ServerUrl);
                        uriBuilder.Path = context.Request.Path;
                        uriBuilder.Query = context.Request.QueryString.Value;
                        using var httpClient = new HttpClient();
                        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), uriBuilder.ToString());
                        request.Content = new StreamContent(context.Request.Body);
                        var resp = await httpClient.SendAsync(request);

                        context.Response.StatusCode = (int)resp.StatusCode;
                        await resp.Content.CopyToAsync(context.Response.Body);
                    }
                }
                await next(context);
            };
        });
    }

    private static async Task Forward(WebSocket ws, WebSocket forwardWebSocket)
    {
        var buffer = new byte[4096];
        while (forwardWebSocket.CloseStatus is null)
        {
            var res = await ws.ReceiveAsync(buffer, cancellationToken: default);
            await forwardWebSocket.SendAsync(buffer.AsMemory(..res.Count), res.MessageType, res.EndOfMessage, cancellationToken: default);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;

namespace AutobahnTestApp;

public class Startup
{
    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseWebSockets();

        var logger = loggerFactory.CreateLogger<Startup>();
        app.Run(async (context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                logger.LogInformation("Received WebSocket request");
                using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext()
                {
                    DangerousEnableCompression = true
                }))
                {
                    await Echo(webSocket, context.RequestAborted);
                }
            }
            else
            {
                var wsScheme = context.Request.IsHttps ? "wss" : "ws";
                var wsUrl = $"{wsScheme}://{context.Request.Host.Host}:{context.Request.Host.Port}{context.Request.Path}";
                await context.Response.WriteAsync($"Ready to accept a WebSocket request at: {wsUrl}");
            }
        });

    }

    private async Task Echo(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        while (!result.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        }
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, cancellationToken);
    }
}

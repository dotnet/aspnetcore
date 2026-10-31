// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http.Features;

namespace EchoApp;

public class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.UseDeveloperExceptionPage();
        app.UseWebSockets();

        app.Use(async (context, next) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext() { DangerousEnableCompression = true });
                await Echo(context, webSocket, app.Logger);
                return;
            }

            await next(context);
        });

        app.UseFileServer();

        return app.RunAsync();
    }

    private static async Task Echo(HttpContext context, WebSocket webSocket, ILogger logger)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(buffer.AsMemory(), CancellationToken.None);
        LogFrame(logger, webSocket, result, buffer);
        while (result.MessageType != WebSocketMessageType.Close)
        {
            // If the client send "ServerClose", then they want a server-originated close to occur
            string content = "<<binary>>";
            if (result.MessageType == WebSocketMessageType.Text)
            {
                content = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (content.Equals("ServerClose"))
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing from Server", CancellationToken.None);
                    logger.LogDebug($"Sent Frame Close: {WebSocketCloseStatus.NormalClosure} Closing from Server");
                    return;
                }
                else if (content.Equals("ServerAbort"))
                {
                    context.Abort();
                }
            }

            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
            logger.LogDebug($"Sent Frame {result.MessageType}: Len={result.Count}, Fin={result.EndOfMessage}: {content}");

            result = await webSocket.ReceiveAsync(buffer.AsMemory(), CancellationToken.None);
            LogFrame(logger, webSocket, result, buffer);
        }
        await webSocket.CloseAsync(webSocket.CloseStatus.Value, webSocket.CloseStatusDescription, CancellationToken.None);
    }

    private static void LogFrame(ILogger logger, WebSocket webSocket, ValueWebSocketReceiveResult frame, byte[] buffer)
    {
        var close = frame.MessageType == WebSocketMessageType.Close;
        string message;
        if (close)
        {
            message = $"Close: {webSocket.CloseStatus.Value} {webSocket.CloseStatusDescription}";
        }
        else
        {
            string content = "<<binary>>";
            if (frame.MessageType == WebSocketMessageType.Text)
            {
                content = Encoding.UTF8.GetString(buffer, 0, frame.Count);
            }
            message = $"{frame.MessageType}: Len={frame.Count}, Fin={frame.EndOfMessage}: {content}";
        }
        logger.LogDebug("Received Frame " + message);
    }
}

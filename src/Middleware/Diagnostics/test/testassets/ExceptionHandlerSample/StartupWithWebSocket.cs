// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;

namespace ExceptionHandlerSample;

// Note that this class isn't used in tests as TestServer doesn't have the right behavior to test web sockets
// in the way we need. But leaving here so it can be used in Program.cs when starting the app manually.
public class StartupWithWebSocket
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseExceptionHandler(options => { }); // Exception handling middleware introduces duplicate tag
        app.UseWebSockets();

        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            try
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var ws = await context.WebSockets.AcceptWebSocketAsync();
                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")), System.Net.WebSockets.WebSocketMessageType.Text, true, context.RequestAborted);
                    await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "done", context.RequestAborted);
                    throw new InvalidOperationException("Throw after websocket request completion to produce the bug");
                }
                else
                {
                    await context.Response.WriteAsync($"Not a web socket request. PID: {Process.GetCurrentProcess().Id}");
                }
            }
            catch (Exception ex)
            {
                _ = ex;
                throw;
            }
        });
    }
}


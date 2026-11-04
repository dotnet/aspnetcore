// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using McpAppsServer;
using System.Net.WebSockets;
using System.Text.Json;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<NoteStore>();
builder.Services.AddMcpServer(options =>
    {
        options.Capabilities ??= new ServerCapabilities();
        options.Capabilities.Extensions = new Dictionary<string, object>
        {
            ["io.modelcontextprotocol/ui"] = JsonSerializer.SerializeToElement(new { mimeTypes = new[] { "text/html;profile=mcp-app" } }),
        };
    })
    .WithHttpTransport()
    .WithTools<NoteTools>()
    .WithResources<NoteResource>();

var app = builder.Build();
app.UseWebSockets();
app.MapMcp("/mcp");
app.MapGet("/state", (NoteStore store) => new
{
    initialCalls = store.InitialCalls,
    savedCalls = store.SavedCalls,
    activeCalls = store.ActiveCalls,
    cancelledCalls = store.CancelledCalls,
    modelOnlyCalls = store.ModelOnlyCalls,
    assetRequests = store.AssetRequests,
    socketRequests = store.SocketRequests,
    message = store.Message,
});
app.MapGet("/asset.svg", (NoteStore store) =>
{
    Interlocked.Increment(ref store.AssetRequests);
    return Results.Content("""<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12"><rect width="12" height="12" fill="green"/></svg>""", "image/svg+xml");
});
app.MapGet("/socket", async (HttpContext context, NoteStore store) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    Interlocked.Increment(ref store.SocketRequests);
    var buffer = new byte[64];
    var received = await socket.ReceiveAsync(buffer.AsMemory(), context.RequestAborted);
    await socket.SendAsync(buffer.AsMemory(0, received.Count), WebSocketMessageType.Text,
        endOfMessage: true, context.RequestAborted);
    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Completed", context.RequestAborted);
});
app.Run();

namespace McpAppsServer
{
    public sealed class ServerAssembly;
}

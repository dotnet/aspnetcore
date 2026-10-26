// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using PlatformTestApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddBrowserPlatform();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PlatformTestApp.Client._Imports).Assembly);

// Records whether the browser aborted each request of the abort link demo, which the page itself cannot see.
var abortLinkRequests = new ConcurrentDictionary<string, string>();
app.MapGet("/abortlink/slow-headers", async (string id, int delay, HttpContext context) =>
{
    abortLinkRequests[id] = "started";
    try
    {
        await Task.Delay(delay, context.RequestAborted);
    }
    catch (OperationCanceledException)
    {
        abortLinkRequests[id] = "aborted";
        return Results.Empty;
    }

    abortLinkRequests[id] = "completed";
    return Results.Text("ok");
});
app.MapGet("/abortlink/slow-body", async (string id, HttpContext context) =>
{
    abortLinkRequests[id] = "started";
    context.Response.ContentType = "text/plain";
    try
    {
        for (var chunk = 0; chunk < 40; chunk++)
        {
            await context.Response.WriteAsync("chunk\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            await Task.Delay(250, context.RequestAborted);
        }
    }
    catch (Exception ex) when (ex is OperationCanceledException or IOException)
    {
        abortLinkRequests[id] = "aborted";
        return;
    }

    abortLinkRequests[id] = "completed";
});
app.MapGet("/abortlink/log/{id}", (string id) =>
    abortLinkRequests.TryGetValue(id, out var state) ? state : "unknown");

app.Run();

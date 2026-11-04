// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using McpAppsHost;
using McpAppsHost.Components;
using Microsoft.AspNetCore.Components.AI.Mcp;

var builder = WebApplication.CreateBuilder(args);
var hostOrigin = new Uri(builder.Configuration["HOST_ORIGIN"]
    ?? throw new InvalidOperationException("HOST_ORIGIN must be configured."));
var sandboxOnly = builder.Configuration.GetValue<bool>("SANDBOX_ONLY");
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(new HostState { HoldResponse = builder.Configuration.GetValue<bool>("HOLD_RESPONSE") });

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
if (sandboxOnly)
{
    app.MapMcpAppProxy("/proxy", hostOrigin);
}
else
{
    app.MapGet("/test-state", (HostState state) => new
    {
        modelRequests = state.ModelRequests,
        receivedViewMetadata = state.ReceivedViewMetadata,
        modelToolNames = state.ModelToolNames,
    });
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
}

app.Run();

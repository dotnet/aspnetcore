// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using McpAppsHost;
using McpAppsHost.Components;
using Microsoft.AspNetCore.Components.AI.Mcp;

var builder = WebApplication.CreateBuilder(args);
var hostOrigin = new Uri(builder.Configuration["HOST_ORIGIN"]
    ?? throw new InvalidOperationException("HOST_ORIGIN must be configured."));
var sandboxOrigin = new Uri(builder.Configuration["SANDBOX_ORIGIN"]
    ?? throw new InvalidOperationException("SANDBOX_ORIGIN must be configured."));
var hostListener = new Uri(builder.Configuration["urls"] ?? hostOrigin.AbsoluteUri);
builder.WebHost.UseUrls(hostListener.AbsoluteUri, sandboxOrigin.AbsoluteUri);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
if (builder.Configuration.GetValue<bool>("LARGE_PAYLOADS"))
{
    builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = null);
}
builder.Services.AddSingleton(new HostState { HoldResponse = builder.Configuration.GetValue<bool>("HOLD_RESPONSE") });

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapMcpAppSandbox<Sandbox>("/mcp-sandbox", hostOrigin, sandboxOrigin,
    parameters: new Dictionary<string, object?>
    {
        [nameof(Sandbox.FrameTitle)] = "MCP app document"
    });
app.MapMcpAppSandbox<McpSandboxBase>("/default-sandbox", hostOrigin, sandboxOrigin);
app.MapGet("/test-state", (HostState state) => new
{
    modelRequests = state.ModelRequests,
    receivedViewMetadata = state.ReceivedViewMetadata,
    modelToolNames = state.ModelToolNames,
});
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().RequireHost(hostListener.Authority);

app.Run();

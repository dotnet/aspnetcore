// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AGUI.Client;
using DojoClient;
using DojoClient.Components;
using DojoClient.Formatting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The dojo UI never talks to a model directly: every scenario goes through AGUIDojoApi over
// HTTP + SSE, which is the boundary these test assets exist to exercise.
var apiBaseUrl = builder.Configuration["AGUI_DOJO_API_URL"] ?? "http://localhost:5018";
builder.Services.AddHttpClient(DojoScenarios.ApiHttpClientName, client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    // Streamed AG-UI responses have no meaningful overall duration limit.
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddScoped<IChatClient>(sp =>
    CreateChatClient(sp, DojoScenarios.AgenticChatEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarios.BackendToolRenderingEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarios.BackendToolRenderingEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarios.HumanInTheLoopEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarios.HumanInTheLoopEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarios.ToolBasedGenerativeUIEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarios.ToolBasedGenerativeUIEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarios.AgenticGenerativeUIEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarios.AgenticGenerativeUIEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarios.SharedStateEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarios.SharedStateEndpoint));

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static IChatClient CreateChatClient(IServiceProvider services, string endpoint)
{
    var httpClient = services.GetRequiredService<IHttpClientFactory>()
        .CreateClient(DojoScenarios.ApiHttpClientName);
    var aguiClient = new AGUIChatClient(new AGUIChatClientOptions(httpClient, endpoint));

    return new FormattedChatClient(aguiClient);
}

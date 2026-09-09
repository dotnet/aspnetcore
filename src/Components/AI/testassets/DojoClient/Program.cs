// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AGUI.Client;
using DojoAgent;
using DojoClient;
using DojoClient.Components;
using DojoClient.Formatting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var backend = new DojoBackend(builder.Configuration["DOJO_BACKEND"]);
builder.Services.AddSingleton(backend);
if (backend.IsDirect)
{
    builder.Services.AddKeyedScoped<IChatClient>(
        ChatClientAgentFactory.ModelServiceKey,
        (sp, _) => ChatClientAgentFactory.CreateAgenticChat(sp.GetRequiredService<IConfiguration>()));
    builder.Services.AddKeyedScoped<IChatClient>(
        ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
        (sp, _) => ChatClientAgentFactory.CreatePredictiveStateUpdates(sp.GetRequiredService<IConfiguration>()));
}
else
{
    var apiBaseUrl = builder.Configuration["AGUI_DOJO_API_URL"] ?? "http://localhost:5018";
    builder.Services.AddHttpClient(DojoScenarios.ApiHttpClientName, client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        // Streamed AG-UI responses have no meaningful overall duration limit.
        client.Timeout = Timeout.InfiniteTimeSpan;
    });
}

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
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarios.PredictiveStateUpdatesEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarios.PredictiveStateUpdatesEndpoint));

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static IChatClient CreateChatClient(IServiceProvider services, string endpoint)
{
    if (services.GetRequiredService<DojoBackend>().IsDirect)
    {
        var key = endpoint == DojoScenarios.PredictiveStateUpdatesEndpoint
            ? ChatClientAgentFactory.PredictiveStateUpdatesServiceKey
            : ChatClientAgentFactory.ModelServiceKey;
        var model = services.GetRequiredKeyedService<IChatClient>(key);
        return new FormattedChatClient(ChatClientAgentFactory.CreateDirect(model, endpoint));
    }

    var httpClient = services.GetRequiredService<IHttpClientFactory>()
        .CreateClient(DojoScenarios.ApiHttpClientName);
    var aguiClient = new AGUIChatClient(new AGUIChatClientOptions(httpClient, endpoint));

    return new FormattedChatClient(aguiClient);
}

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
builder.Services.AddSingleton<FunctionScenarioState>();
builder.Services.AddScoped(sp => new FunctionScenarioControls(async threadId =>
{
    if (backend.IsDirect)
    {
        sp.GetRequiredService<FunctionScenarioState>().ReleaseResult(threadId);
    }
    else
    {
        using var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient(DojoScenarios.ApiHttpClientName);
        using var response = await client.PostAsync(
            $"/_test/functions/{Uri.EscapeDataString(threadId)}/release", content: null);
        response.EnsureSuccessStatusCode();
    }
}));
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
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Approval,
    (sp, _) => CreateChatClient(sp, FunctionScenarios.Approval));
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Invocation,
    (sp, _) => CreateChatClient(sp, FunctionScenarios.Invocation));
builder.Services.AddKeyedScoped<IChatClient>(StructuredRichTextChatClient.Endpoint,
    (sp, _) => CreateChatClient(sp, StructuredRichTextChatClient.Endpoint));
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
if (backend.IsDirect)
{
    app.MapFunctionScenarioControls();
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static IChatClient CreateChatClient(IServiceProvider services, string endpoint)
{
    if (services.GetRequiredService<DojoBackend>().IsDirect)
    {
        if (endpoint == StructuredRichTextChatClient.Endpoint)
        {
            return StructuredRichTextChatClient.Create();
        }

        if (endpoint is FunctionScenarios.Approval or FunctionScenarios.Invocation)
        {
            return new FormattedChatClient(FunctionScenarios.Create(
                services.GetRequiredService<FunctionScenarioState>(),
                requiresApproval: endpoint == FunctionScenarios.Approval));
        }

        var key = endpoint == DojoScenarios.PredictiveStateUpdatesEndpoint
            ? ChatClientAgentFactory.PredictiveStateUpdatesServiceKey
            : ChatClientAgentFactory.ModelServiceKey;
        var model = services.GetRequiredKeyedService<IChatClient>(key);
        return new FormattedChatClient(ChatClientAgentFactory.CreateDirect(model, endpoint));
    }

    var httpClient = services.GetRequiredService<IHttpClientFactory>()
        .CreateClient(DojoScenarios.ApiHttpClientName);
    IChatClient aguiClient = new AGUIChatClient(new AGUIChatClientOptions(httpClient, endpoint));
    if (endpoint is StructuredRichTextChatClient.Endpoint or FunctionScenarios.Invocation)
    {
        aguiClient = new DojoContentAGUIChatClient(aguiClient);
    }

    return new FormattedChatClient(aguiClient);
}

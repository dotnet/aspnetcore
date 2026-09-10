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

var backend = DojoBackendConfiguration.Parse(builder.Configuration["DOJO_BACKEND"]);
IDojoScenarioBridge scenarioBridge = backend == DojoBackendKind.Direct
    ? new DirectDojoScenarioBridge()
    : new AGUIDojoScenarioBridge();
builder.Services.AddSingleton(scenarioBridge);
builder.Services.AddSingleton<FunctionScenarioState>();
builder.Services.AddScoped(sp => new FunctionScenarioControls(async threadId =>
{
    if (backend == DojoBackendKind.Direct)
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
if (backend == DojoBackendKind.Direct)
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

// E2E tests decorate these registrations (see DojoModelOverrides.ConfigureUI); each must stay
// a factory registration so the decorator can rebuild the scenario's CreateChatClient pipeline
// around a replaced model, instead of wrapping a fixed instance.
builder.Services.AddScoped<IChatClient>(sp =>
    CreateChatClient(sp, DojoScenarioEndpoints.AgenticChatEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Approval,
    (sp, _) => CreateChatClient(sp, FunctionScenarios.Approval));
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Invocation,
    (sp, _) => CreateChatClient(sp, FunctionScenarios.Invocation));
builder.Services.AddKeyedScoped<IChatClient>(StructuredRichTextChatClient.Endpoint,
    (sp, _) => CreateChatClient(sp, StructuredRichTextChatClient.Endpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarioEndpoints.BackendToolRenderingEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarioEndpoints.BackendToolRenderingEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarioEndpoints.HumanInTheLoopEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarioEndpoints.HumanInTheLoopEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarioEndpoints.ToolBasedGenerativeUIEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarioEndpoints.ToolBasedGenerativeUIEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarioEndpoints.AgenticGenerativeUIEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarioEndpoints.AgenticGenerativeUIEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarioEndpoints.SharedStateEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarioEndpoints.SharedStateEndpoint));
builder.Services.AddKeyedScoped<IChatClient>(
    DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint,
    (sp, _) => CreateChatClient(sp, DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint));

var app = builder.Build();

app.UseAntiforgery();
if (backend == DojoBackendKind.Direct)
{
    app.MapFunctionScenarioControls();
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

IChatClient CreateChatClient(IServiceProvider services, string endpoint)
{
    if (backend == DojoBackendKind.Direct)
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

        var key = endpoint == DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint
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

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
var directBridge = new DirectDojoScenarioBridge();
builder.Services.AddSingleton(directBridge);
IDojoScenarioBridge scenarioBridge = backend == DojoBackendKind.Direct
    ? directBridge
    : new AGUIDojoScenarioBridge();
builder.Services.AddSingleton(scenarioBridge);
builder.Services.AddSingleton<FunctionScenarioState>();
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Invocation,
    (sp, _) => new FormattedChatClient(
        FunctionScenarios.Create(sp.GetRequiredService<FunctionScenarioState>(), requiresApproval: false)));
builder.Services.AddKeyedScoped<IChatClient>(StructuredRichTextChatClient.Endpoint,
    (_, _) => StructuredRichTextChatClient.Create());
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
        client.Timeout = Timeout.InfiniteTimeSpan;
    });
}

foreach (var scenario in DojoScenarioCatalog.All)
{
    if (scenario.Endpoint == DojoScenarioEndpoints.AgenticChatEndpoint)
    {
        builder.Services.AddScoped<IChatClient>(sp => CreateChatClient(sp, scenario.Endpoint));
    }
    else
    {
        builder.Services.AddKeyedScoped<IChatClient>(scenario.Endpoint,
            (sp, _) => CreateChatClient(sp, scenario.Endpoint));
    }
}
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Approval,
    (sp, _) => CreateChatClient(sp, FunctionScenarios.Approval));

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
        if (endpoint == FunctionScenarios.Approval)
        {
            return new FormattedChatClient(FunctionScenarios.Create(
                services.GetRequiredService<FunctionScenarioState>(),
                requiresApproval: true));
        }

        var key = DojoScenarioCatalog.Get(endpoint).ModelServiceKey ?? ChatClientAgentFactory.ModelServiceKey;
        var model = services.GetRequiredKeyedService<IChatClient>(key);
        return new FormattedChatClient(ChatClientAgentFactory.CreateDirect(model, endpoint));
    }

    var httpClient = services.GetRequiredService<IHttpClientFactory>()
        .CreateClient(DojoScenarios.ApiHttpClientName);
    var aguiClient = new AGUIChatClient(new AGUIChatClientOptions(httpClient, endpoint));

    return new FormattedChatClient(aguiClient);
}

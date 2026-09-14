// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using AGUI.Formatting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using AGUIDojoApi;
using DojoAgent;

using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IAGUIEventStreamFormatter, SseEventStreamFormatter>());
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(AIJsonUtilities.DefaultOptions.TypeInfoResolver!);
    options.SerializerOptions.TypeInfoResolverChain.Add(AGUIJsonSerializerContext.Default);
    AGUIJsonUtilities.RegisterInterruptContentTypes(options.SerializerOptions);
});

builder.Services.AddSingleton<IChatClient>(sp =>
    ChatClientAgentFactory.CreateAgenticChat(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<FunctionScenarioState>();
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Approval,
    (sp, _) => FunctionScenarios.Create(sp.GetRequiredService<FunctionScenarioState>(), requiresApproval: true));
builder.Services.AddKeyedSingleton<IChatClient>(
    ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
    (sp, _) => ChatClientAgentFactory.CreatePredictiveStateUpdates(
        sp.GetRequiredService<IConfiguration>()));

var app = builder.Build();
var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>();

foreach (var scenario in DojoScenarioCatalog.All)
{
    app.MapDojoEndpoint(
        scenario.Endpoint,
        serverTools: scenario.CreateServerTools(jsonOptions.Value.SerializerOptions),
        systemPrompt: scenario.SystemPrompt,
        configureStreamOptions: scenario.CreateStreamOptions,
        chatClientKey: scenario.ModelServiceKey,
        treatClientToolsAsDeclarations: scenario.TreatClientToolsAsDeclarations);
}
app.MapDojoEndpoint(FunctionScenarios.Approval, chatClientKey: FunctionScenarios.Approval);
app.MapFunctionScenarioControls();

await app.RunAsync();

public partial class Program;

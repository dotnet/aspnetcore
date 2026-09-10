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
builder.Services.AddKeyedScoped<IChatClient>(FunctionScenarios.Invocation,
    (sp, _) => DojoContentTransport.WrapServer(
        FunctionScenarios.Create(sp.GetRequiredService<FunctionScenarioState>(), requiresApproval: false)));
builder.Services.AddKeyedScoped<IChatClient>(StructuredRichTextChatClient.Endpoint,
    (_, _) => DojoContentTransport.WrapServer(StructuredRichTextChatClient.Create()));
builder.Services.AddKeyedSingleton<IChatClient>(
    ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
    (sp, _) => ChatClientAgentFactory.CreatePredictiveStateUpdates(
        sp.GetRequiredService<IConfiguration>()));

var app = builder.Build();
var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>();

app.MapDojoEndpoint(DojoScenarioEndpoints.AgenticChatEndpoint);
app.MapDojoEndpoint(FunctionScenarios.Approval, chatClientKey: FunctionScenarios.Approval);
app.MapDojoEndpoint(FunctionScenarios.Invocation, chatClientKey: FunctionScenarios.Invocation);
app.MapDojoEndpoint(StructuredRichTextChatClient.Endpoint, chatClientKey: StructuredRichTextChatClient.Endpoint);
app.MapFunctionScenarioControls();
app.MapDojoEndpoint(
    DojoScenarioEndpoints.BackendToolRenderingEndpoint,
    serverTools: ChatClientAgentFactory.CreateBackendToolRenderingTools(
        jsonOptions.Value.SerializerOptions));
app.MapDojoEndpoint(
    DojoScenarioEndpoints.HumanInTheLoopEndpoint,
    systemPrompt: ChatClientAgentFactory.HumanInTheLoopSystemPrompt);
app.MapDojoEndpoint(
    DojoScenarioEndpoints.ToolBasedGenerativeUIEndpoint,
    systemPrompt: ChatClientAgentFactory.ToolBasedGenerativeUISystemPrompt);
app.MapDojoEndpoint(
    DojoScenarioEndpoints.AgenticGenerativeUIEndpoint,
    serverTools: ChatClientAgentFactory.CreateAgenticGenerativeUITools(
        jsonOptions.Value.SerializerOptions),
    systemPrompt: ChatClientAgentFactory.AgenticGenerativeUISystemPrompt,
    configureStreamOptions: _ =>
        ChatClientAgentFactory.CreateAgenticGenerativeUIStreamOptions());
app.MapDojoEndpoint(
    DojoScenarioEndpoints.SharedStateEndpoint,
    serverTools: ChatClientAgentFactory.CreateSharedStateTools(
        jsonOptions.Value.SerializerOptions),
    systemPrompt: ChatClientAgentFactory.SharedStateSystemPrompt,
    configureStreamOptions: _ => ChatClientAgentFactory.CreateSharedStateStreamOptions());
app.MapDojoEndpoint(
    DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint,
    serverTools: ChatClientAgentFactory.CreatePredictiveStateUpdatesTools(
        jsonOptions.Value.SerializerOptions),
    systemPrompt: ChatClientAgentFactory.PredictiveStateUpdatesSystemPrompt,
    configureStreamOptions: ChatClientAgentFactory.CreatePredictiveStateUpdatesStreamOptions,
    chatClientKey: ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
    treatClientToolsAsDeclarations: true);

await app.RunAsync();

public partial class Program;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using AGUI.Formatting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using AGUIDojoApi;

using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

// AG-UI hosting: the SSE formatter plus the JSON configuration the protocol types need.
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
builder.Services.AddKeyedSingleton<IChatClient>(
    ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
    (sp, _) => ChatClientAgentFactory.CreatePredictiveStateUpdates(
        sp.GetRequiredService<IConfiguration>()));

var app = builder.Build();
var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>();

// Map the AG-UI agent endpoints for the dojo scenarios.
app.MapDojoEndpoint("/agentic_chat");
app.MapDojoEndpoint(
    "/backend_tool_rendering",
    serverTools: ChatClientAgentFactory.CreateBackendToolRenderingTools(
        jsonOptions.Value.SerializerOptions));
app.MapDojoEndpoint(
    "/human_in_the_loop",
    systemPrompt: ChatClientAgentFactory.HumanInTheLoopSystemPrompt);
app.MapDojoEndpoint(
    "/tool_based_generative_ui",
    systemPrompt: ChatClientAgentFactory.ToolBasedGenerativeUISystemPrompt);
app.MapDojoEndpoint(
    "/agentic_generative_ui",
    serverTools: ChatClientAgentFactory.CreateAgenticGenerativeUITools(
        jsonOptions.Value.SerializerOptions),
    systemPrompt: ChatClientAgentFactory.AgenticGenerativeUISystemPrompt,
    configureStreamOptions: _ =>
        ChatClientAgentFactory.CreateAgenticGenerativeUIStreamOptions());
app.MapDojoEndpoint(
    "/shared_state",
    serverTools: ChatClientAgentFactory.CreateSharedStateTools(
        jsonOptions.Value.SerializerOptions),
    systemPrompt: ChatClientAgentFactory.SharedStateSystemPrompt,
    configureStreamOptions: _ => ChatClientAgentFactory.CreateSharedStateStreamOptions());
app.MapDojoEndpoint(
    "/predictive_state_updates",
    serverTools: ChatClientAgentFactory.CreatePredictiveStateUpdatesTools(
        jsonOptions.Value.SerializerOptions),
    systemPrompt: ChatClientAgentFactory.PredictiveStateUpdatesSystemPrompt,
    configureStreamOptions: ChatClientAgentFactory.CreatePredictiveStateUpdatesStreamOptions,
    chatClientKey: ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
    treatClientToolsAsDeclarations: true);

await app.RunAsync();

public partial class Program;

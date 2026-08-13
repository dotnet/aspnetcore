// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using AGUI.Formatting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

var app = builder.Build();

// Map the AG-UI agent endpoints for the dojo scenarios.
app.MapDojoEndpoint("/agentic_chat");

await app.RunAsync();

public partial class Program;

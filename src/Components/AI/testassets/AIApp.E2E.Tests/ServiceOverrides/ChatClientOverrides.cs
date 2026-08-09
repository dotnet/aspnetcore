// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AIApp.E2E.Tests.ServiceOverrides;

// Service override methods for E2E tests.
// Each method replaces the app's IChatClient with a baseline replay client.
// Registered via options.ConfigureServices<ChatClientOverrides>(nameof(...))
//
// The replay client is registered as scoped so every circuit gets a fresh one. The
// server fixture reuses a single app process for all tests that request the same
// override, and a replay client can only serve the turns it recorded, so a shared
// singleton would be exhausted by the first test that sends a message.
class ChatClientOverrides
{
    public static void AgenticChat(IServiceCollection services)
    {
        services.AddScoped(_ => ReplayCheckpointScript.Load("Dojo_AgenticChat.recording.json"));
        services.AddScoped<IChatClient, GatedReplayChatClient>();
    }

    public static void BackendToolRendering(IServiceCollection services)
    {
        services.AddScoped(_ => ReplayCheckpointScript.Load("Dojo_BackendToolRendering.recording.json"));
        services.AddScoped<IChatClient, GatedReplayChatClient>();
    }

    public static void HumanInTheLoop(IServiceCollection services)
    {
        services.AddScoped(_ => ReplayCheckpointScript.Load("Dojo_HumanInTheLoop.recording.json"));
        services.AddScoped<IChatClient, GatedReplayChatClient>();
    }

    public static void SingleTurnEcho(IServiceCollection services)
    {
        services.AddScoped<IChatClient>(
            _ => BaselineReplayClient.FromBaseline("E2E_SingleTurnEcho.recording.json"));
    }

    public static void MultiTokenStreaming(IServiceCollection services)
    {
        services.AddScoped<IChatClient>(
            _ => BaselineReplayClient.FromBaseline("E2E_MultiTokenStreaming.recording.json"));
    }

    public static void MultiTurn(IServiceCollection services)
    {
        services.AddScoped<IChatClient>(
            _ => BaselineReplayClient.FromBaseline("E2E_MultiTurn.recording.json"));
    }
}

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
        => AddReplay(services, "Dojo_AgenticChat.recording.json");

    public static void BackendToolRendering(IServiceCollection services)
        => AddReplay(services, "Dojo_BackendToolRendering.recording.json");

    public static void HumanInTheLoop(IServiceCollection services)
        => AddReplay(services, "Dojo_HumanInTheLoop.recording.json");

    public static void ToolBasedGenerativeUI(IServiceCollection services)
        => AddReplay(services, "Dojo_ToolBasedGenerativeUI.recording.json");

    public static void AgenticGenerativeUI(IServiceCollection services)
        => AddReplay(services, "Dojo_AgenticGenerativeUI.recording.json");

    public static void SharedState(IServiceCollection services)
        => AddReplay(services, "Dojo_SharedState.recording.json");

    public static void PredictiveStateUpdates(IServiceCollection services)
        => AddReplay(services, "Dojo_PredictiveStateUpdates.recording.json");

    public static void SingleTurnEcho(IServiceCollection services)
        => AddReplay(services, "E2E_SingleTurnEcho.recording.json");

    public static void MultiTokenStreaming(IServiceCollection services)
        => AddReplay(services, "E2E_MultiTokenStreaming.recording.json");

    public static void MultiTurn(IServiceCollection services)
        => AddReplay(services, "E2E_MultiTurn.recording.json");

    private static void AddReplay(IServiceCollection services, string recordingFileName)
    {
        services.AddScoped(_ => ReplayCheckpointScript.Load(recordingFileName));
        services.AddScoped<IChatClient, GatedReplayChatClient>();
    }
}

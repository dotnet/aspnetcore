// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AIApp.E2E.Tests.ServiceOverrides;

// Service override methods for E2E tests.
// Each method replaces the app's IChatClient with a baseline replay client.
// Registered via options.ConfigureServices<ChatClientOverrides>(nameof(...))
class ChatClientOverrides
{
    public static void SingleTurnEcho(IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(
            BaselineReplayClient.FromBaseline("E2E_SingleTurnEcho.recording.json"));
    }

    public static void MultiTokenStreaming(IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(
            BaselineReplayClient.FromBaseline("E2E_MultiTokenStreaming.recording.json"));
    }

    public static void MultiTurn(IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(
            BaselineReplayClient.FromBaseline("E2E_MultiTurn.recording.json"));
    }
}

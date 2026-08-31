// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DojoClient.E2E.Tests.ServiceOverrides;

// Service override methods applied to AGUIDojoApi (never to DojoClient: replacing the UI's
// AGUIChatClient would remove the AG-UI transport from the test).
// Registered via options.ConfigureServices<DojoModelOverrides>(nameof(...)).
internal class DojoModelOverrides
{
    public static void AgenticChat(IServiceCollection services)
        => AddRecordedModel(services, "AgenticChat.recording.json");

    public static void AgenticChatRichText(IServiceCollection services)
        => AddRecordedModel(services, "AgenticChatRichText.recording.json");

    public static void AgenticChatClientTool(IServiceCollection services)
        => AddRecordedModel(services, "AgenticChatClientTool.recording.json");

    public static void BackendToolRendering(IServiceCollection services)
    {
        services.AddSingleton(_ => RecordedScript.Load("BackendToolRendering.recording.json"));
        services.AddScoped<RecordedChatClient>();
        services.AddScoped<IChatClient>(sp =>
            new FunctionInvokingChatClient(sp.GetRequiredService<RecordedChatClient>()));
    }

    public static void HumanInTheLoop(IServiceCollection services)
        => AddRecordedModel(services, "HumanInTheLoop.recording.json");

    public static void ToolBasedGenerativeUI(IServiceCollection services)
        => AddRecordedModel(services, "ToolBasedGenerativeUI.recording.json");

    public static void AgenticGenerativeUI(IServiceCollection services)
    {
        services.AddSingleton(_ => RecordedScript.Load("AgenticGenerativeUI.recording.json"));
        services.AddScoped<RecordedChatClient>();
        services.AddScoped<IChatClient>(sp =>
            new FunctionInvokingChatClient(sp.GetRequiredService<RecordedChatClient>()));
    }

    public static void SharedState(IServiceCollection services)
    {
        services.AddSingleton(_ => RecordedScript.Load("SharedState.recording.json"));
        services.AddScoped<RecordedChatClient>();
        services.AddScoped<IChatClient>(sp =>
            new FunctionInvokingChatClient(sp.GetRequiredService<RecordedChatClient>()));
    }

    private static void AddRecordedModel(IServiceCollection services, string recordingFileName)
    {
        services.AddSingleton(_ => RecordedScript.Load(recordingFileName));
        services.AddScoped<IChatClient, RecordedChatClient>();
    }
}

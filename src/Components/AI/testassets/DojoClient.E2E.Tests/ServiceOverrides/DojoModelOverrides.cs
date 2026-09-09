// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DojoClient.E2E.Tests.ServiceOverrides;

// Replace only the model in its owning host, leaving both backend pipelines intact.
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
        => AddRecordedModel(services, "BackendToolRendering.recording.json", invokeFunctions: true);

    public static void HumanInTheLoop(IServiceCollection services)
        => AddRecordedModel(services, "HumanInTheLoop.recording.json");

    public static void ToolBasedGenerativeUI(IServiceCollection services)
        => AddRecordedModel(services, "ToolBasedGenerativeUI.recording.json");

    public static void AgenticGenerativeUI(IServiceCollection services)
        => AddRecordedModel(services, "AgenticGenerativeUI.recording.json", invokeFunctions: true);

    public static void SharedState(IServiceCollection services)
        => AddRecordedModel(services, "SharedState.recording.json", invokeFunctions: true);

    public static void PredictiveStateUpdates(IServiceCollection services)
    {
        services.AddSingleton(_ => RecordedScript.Load("PredictiveStateUpdates.recording.json"));
        services.AddScoped<RecordedChatClient>();
        services.AddKeyedScoped<IChatClient>(
            ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
            (sp, _) => sp.GetRequiredService<RecordedChatClient>());
    }

    private static void AddRecordedModel(
        IServiceCollection services,
        string recordingFileName,
        bool invokeFunctions = false)
    {
        services.AddSingleton(_ => RecordedScript.Load(recordingFileName));
        services.AddScoped<RecordedChatClient>();
        if (Environment.GetEnvironmentVariable("DOJO_BACKEND") == "Direct")
        {
            if (!services.Any(service =>
                service.ServiceType == typeof(IChatClient) &&
                Equals(service.ServiceKey, ChatClientAgentFactory.ModelServiceKey)))
            {
                throw new InvalidOperationException("The direct dojo model registration is missing.");
            }

            services.AddKeyedScoped<IChatClient>(
                ChatClientAgentFactory.ModelServiceKey, (sp, _) => CreateModel(sp));
        }
        else
        {
            services.AddScoped<IChatClient>(CreateModel);
        }

        IChatClient CreateModel(IServiceProvider services)
        {
            var model = services.GetRequiredService<RecordedChatClient>();

            return invokeFunctions ? new FunctionInvokingChatClient(model) : model;
        }
    }
}

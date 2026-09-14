// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoAgent;

/// <summary>
/// Provides the standard scenario configurations shared by the dojo hosts.
/// </summary>
public static class DojoScenarioCatalog
{
    /// <summary>
    /// Gets the standard scenarios in endpoint registration order.
    /// </summary>
    public static IReadOnlyList<DojoScenarioDescriptor> All { get; } = Array.AsReadOnly<DojoScenarioDescriptor>(
    [
        new(DojoScenarioEndpoints.AgenticChatEndpoint),
        new(DojoScenarioEndpoints.BackendToolRenderingEndpoint,
            createServerTools: ChatClientAgentFactory.CreateBackendToolRenderingTools),
        new(DojoScenarioEndpoints.HumanInTheLoopEndpoint,
            systemPrompt: ChatClientAgentFactory.HumanInTheLoopSystemPrompt),
        new(DojoScenarioEndpoints.ToolBasedGenerativeUIEndpoint,
            systemPrompt: ChatClientAgentFactory.ToolBasedGenerativeUISystemPrompt),
        new(DojoScenarioEndpoints.AgenticGenerativeUIEndpoint,
            systemPrompt: ChatClientAgentFactory.AgenticGenerativeUISystemPrompt,
            createServerTools: ChatClientAgentFactory.CreateAgenticGenerativeUITools,
            createStreamOptions: _ => ChatClientAgentFactory.CreateAgenticGenerativeUIStreamOptions()),
        new(DojoScenarioEndpoints.SharedStateEndpoint,
            systemPrompt: ChatClientAgentFactory.SharedStateSystemPrompt,
            createServerTools: ChatClientAgentFactory.CreateSharedStateTools,
            createStreamOptions: _ => ChatClientAgentFactory.CreateSharedStateStreamOptions()),
        new(DojoScenarioEndpoints.PredictiveStateUpdatesEndpoint,
            systemPrompt: ChatClientAgentFactory.PredictiveStateUpdatesSystemPrompt,
            createServerTools: ChatClientAgentFactory.CreatePredictiveStateUpdatesTools,
            createStreamOptions: ChatClientAgentFactory.CreatePredictiveStateUpdatesStreamOptions,
            modelServiceKey: ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
            predictiveStateUpdates: true,
            treatClientToolsAsDeclarations: true),
    ]);

    /// <summary>
    /// Gets a standard scenario by its endpoint path.
    /// </summary>
    /// <param name="endpoint">The scenario's endpoint path.</param>
    /// <returns>The shared scenario descriptor.</returns>
    /// <exception cref="ArgumentException">The endpoint is not a standard dojo scenario.</exception>
    public static DojoScenarioDescriptor Get(string endpoint)
        => All.FirstOrDefault(scenario => scenario.Endpoint == endpoint)
            ?? throw new ArgumentException($"Unknown dojo scenario '{endpoint}'.", nameof(endpoint));
}

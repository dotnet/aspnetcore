// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using AGUI.Server;
using Microsoft.Extensions.AI;

namespace DojoAgent;

/// <summary>
/// Describes a standard scenario shared by the direct and AG-UI dojo hosts.
/// </summary>
public sealed class DojoScenarioDescriptor
{
    internal DojoScenarioDescriptor(
        string endpoint,
        string? systemPrompt = null,
        Func<JsonSerializerOptions, IList<AITool>>? createServerTools = null,
        Func<JsonSerializerOptions, AGUIStreamOptions>? createStreamOptions = null,
        string? modelServiceKey = null,
        bool predictiveStateUpdates = false,
        bool treatClientToolsAsDeclarations = false)
    {
        Endpoint = endpoint;
        SystemPrompt = systemPrompt;
        CreateServerTools = createServerTools ?? (_ => []);
        CreateStreamOptions = createStreamOptions;
        ModelServiceKey = modelServiceKey;
        PredictiveStateUpdates = predictiveStateUpdates;
        TreatClientToolsAsDeclarations = treatClientToolsAsDeclarations;
    }

    /// <summary>
    /// Gets the scenario's endpoint path.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the system prompt to prepend, or <see langword="null"/> for no additional prompt.
    /// </summary>
    public string? SystemPrompt { get; }

    /// <summary>
    /// Gets a factory that creates server tools with the host's serialization options.
    /// </summary>
    public Func<JsonSerializerOptions, IList<AITool>> CreateServerTools { get; }

    /// <summary>
    /// Gets a per-request AG-UI stream options factory, or <see langword="null"/> for default options.
    /// </summary>
    public Func<JsonSerializerOptions, AGUIStreamOptions>? CreateStreamOptions { get; }

    /// <summary>
    /// Gets the model service key override, or <see langword="null"/> to use the host's default
    /// model: <see cref="ChatClientAgentFactory.ModelServiceKey"/> for direct clients and
    /// the unkeyed model for the AG-UI API.
    /// </summary>
    public string? ModelServiceKey { get; }

    /// <summary>
    /// Gets whether direct clients emit predictive document state updates.
    /// </summary>
    public bool PredictiveStateUpdates { get; }

    /// <summary>
    /// Gets whether the AG-UI host passes client tools to the model as declarations.
    /// </summary>
    public bool TreatClientToolsAsDeclarations { get; }
}

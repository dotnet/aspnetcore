// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoAgent;

/// <summary>
/// The AGUIDojoApi endpoint paths shared by the AG-UI and direct dojo hosts.
/// </summary>
public static class DojoScenarioEndpoints
{
    /// <summary>The agentic chat scenario's endpoint.</summary>
    public const string AgenticChatEndpoint = "/agentic_chat";

    /// <summary>The backend tool rendering scenario's endpoint.</summary>
    public const string BackendToolRenderingEndpoint = "/backend_tool_rendering";

    /// <summary>The human-in-the-loop scenario's endpoint.</summary>
    public const string HumanInTheLoopEndpoint = "/human_in_the_loop";

    /// <summary>The tool-based generative UI scenario's endpoint.</summary>
    public const string ToolBasedGenerativeUIEndpoint = "/tool_based_generative_ui";

    /// <summary>The agentic generative UI scenario's endpoint.</summary>
    public const string AgenticGenerativeUIEndpoint = "/agentic_generative_ui";

    /// <summary>The shared state scenario's endpoint.</summary>
    public const string SharedStateEndpoint = "/shared_state";

    /// <summary>The predictive state updates scenario's endpoint.</summary>
    public const string PredictiveStateUpdatesEndpoint = "/predictive_state_updates";
}

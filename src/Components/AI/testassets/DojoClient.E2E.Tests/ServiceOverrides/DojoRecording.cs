// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoClient.E2E.Tests.ServiceOverrides;

/// <summary>Recorded model scenarios available to an individual dojo test session.</summary>
public enum DojoRecording
{
    /// <summary>Incremental and multi-turn chat.</summary>
    AgenticChat,
    /// <summary>Formatted chat content.</summary>
    AgenticChatRichText,
    /// <summary>Client tool invocation and continuation.</summary>
    AgenticChatClientTool,
    /// <summary>Server weather tool execution.</summary>
    BackendToolRendering,
    /// <summary>User selection of generated task steps.</summary>
    HumanInTheLoop,
    /// <summary>Haiku generation through a UI tool.</summary>
    ToolBasedGenerativeUI,
    /// <summary>Incremental plan creation and updates.</summary>
    AgenticGenerativeUI,
    /// <summary>Shared recipe state.</summary>
    SharedState,
    /// <summary>Predictive document editing.</summary>
    PredictiveStateUpdates,
}

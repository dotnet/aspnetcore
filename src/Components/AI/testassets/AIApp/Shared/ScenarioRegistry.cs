// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AIApp.Shared;

internal sealed class ScenarioRegistry
{
    private static readonly ScenarioDescriptor[] _scenarios =
    [
        new ScenarioDescriptor
        {
            Id = "agentic_chat",
            Title = "Agentic Chat",
            Description = "Chat with your Copilot and call frontend tools",
            Tags = ["Chat", "Tools", "Streaming"],
            Icon = "💬"
        },
        new ScenarioDescriptor
        {
            Id = "backend_tool_rendering",
            Title = "Backend Tool Rendering",
            Description = "Render and stream your backend tools to the frontend.",
            Tags = ["Agent State", "Collaborating"],
            Icon = "🌤️"
        },
        new ScenarioDescriptor
        {
            Id = "human_in_the_loop",
            Title = "Human in the loop",
            Description = "Plan a task together and direct the Copilot to take the right steps",
            Tags = ["HITL", "Interactivity"],
            Icon = "👤"
        },
        new ScenarioDescriptor
        {
            Id = "tool_based_generative_ui",
            Title = "Tool Based Generative UI",
            Description = "Haiku generator that uses tool based generative UI.",
            Tags = ["Generative ui (action)", "Tools"],
            Icon = "🌸"
        },
        new ScenarioDescriptor
        {
            Id = "agentic_generative_ui",
            Title = "Agentic Generative UI",
            Description = "Assign a long running task to your Copilot and see how it performs!",
            Tags = ["Generative ui (agent)", "Long running task"],
            Icon = "📋"
        },
        new ScenarioDescriptor
        {
            Id = "shared_state",
            Title = "Shared State between agent and UI",
            Description = "A recipe Copilot which reads and updates collaboratively",
            Tags = ["Agent State", "Collaborating"],
            Icon = "🍳"
        },
        new ScenarioDescriptor
        {
            Id = "predictive_state_updates",
            Title = "Predictive State Updates",
            Description = "Use collaboration to edit a document in real time with your Copilot",
            Tags = ["State", "Streaming", "Tools"],
            Icon = "📝"
        },
    ];

    public IReadOnlyList<ScenarioDescriptor> AllScenarios => _scenarios;

    public ScenarioDescriptor? Find(string id) =>
        Array.Find(_scenarios, s => s.Id == id);
}

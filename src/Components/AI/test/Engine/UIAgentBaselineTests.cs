// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

// Replays responses recorded from a real provider, so the engine is exercised against the
// update shapes a live model produces (empty first update, per-token text, trailing usage).
public class UIAgentBaselineTests
{
    [Fact]
    public async Task SingleTurnRecording_ProducesOneCompletedTextBlock()
    {
        var client = RecordingLoader.CreateReplayClient("TextStreaming_SingleTurn.recording.json");
        var agent = new UIAgent(client);

        var blocks = await CollectAsync(agent, "Hello");

        var assistant = Assert.IsType<RichContentBlock>(
            Assert.Single(blocks.Where(block => block.Role == ChatRole.Assistant)));
        Assert.Equal("Hello — I hope you're having a great day!", assistant.RawText);
        Assert.Equal(BlockLifecycleState.Inactive, assistant.LifecycleState);
    }

    [Fact]
    public async Task MultiTurnRecording_ProducesOneTextBlockPerTurn()
    {
        var client = RecordingLoader.CreateReplayClient("TextStreaming_MultiTurn.recording.json");
        var agent = new UIAgent(client);

        var firstTurn = await CollectAsync(agent, "Hello");
        var secondTurn = await CollectAsync(agent, "Goodbye");

        Assert.Equal("Hello!", GetAssistantText(firstTurn));
        Assert.Equal("Goodbye!", GetAssistantText(secondTurn));
    }

    [Fact]
    public async Task MultiTurnRecording_TurnsHaveDistinctBlockIds()
    {
        var client = RecordingLoader.CreateReplayClient("TextStreaming_MultiTurn.recording.json");
        var agent = new UIAgent(client);

        var firstTurn = await CollectAsync(agent, "Hello");
        var secondTurn = await CollectAsync(agent, "Goodbye");

        var firstId = firstTurn.Single(block => block.Role == ChatRole.Assistant).Id;
        var secondId = secondTurn.Single(block => block.Role == ChatRole.Assistant).Id;
        Assert.NotEqual(firstId, secondId);
    }

    private static string GetAssistantText(List<ContentBlock> blocks)
        => Assert.IsType<RichContentBlock>(
            Assert.Single(blocks.Where(block => block.Role == ChatRole.Assistant))).RawText;

    private static async Task<List<ContentBlock>> CollectAsync(UIAgent agent, string text)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(new ChatMessage(ChatRole.User, text)))
        {
            blocks.Add(block);
        }

        return blocks;
    }
}

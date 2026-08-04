// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentBaselineTests
{
    [Fact]
    public async Task Step01_MultiTurnTextStreaming_ProducesCorrectBlocks()
    {
        var client = RecordingLoader.CreateReplayClient(
            "Step01_GettingStartedTest.PostRun_MultiTurn_SynthesizesAssistantMessages.recording.json");

        var agent = new UIAgent(client);

        // Turn 1
        var turn1Blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hello")))
        {
            turn1Blocks.Add(block);
        }

        var assistantBlocks1 = turn1Blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.NotEmpty(assistantBlocks1);
        var text1 = Assert.IsType<RichContentBlock>(assistantBlocks1[0]);
        Assert.False(string.IsNullOrEmpty(text1.RawText));
        Assert.Equal(BlockLifecycleState.Inactive, text1.LifecycleState);

        // Turn 2
        var turn2Blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Tell me more")))
        {
            turn2Blocks.Add(block);
        }

        var assistantBlocks2 = turn2Blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.NotEmpty(assistantBlocks2);
        var text2 = Assert.IsType<RichContentBlock>(assistantBlocks2[0]);
        Assert.False(string.IsNullOrEmpty(text2.RawText));
        Assert.Equal(BlockLifecycleState.Inactive, text2.LifecycleState);
    }
}

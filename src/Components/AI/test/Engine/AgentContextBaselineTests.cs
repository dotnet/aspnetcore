// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextBaselineTests
{
    [Fact]
    public async Task Step01_MultiTurnTextStreaming_FullOrchestration()
    {
        var client = RecordingLoader.CreateReplayClient(
            "Step01_GettingStartedTest.PostRun_MultiTurn_SynthesizesAssistantMessages.recording.json");
        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        var turnCount = 0;
        var blockCount = 0;
        context.RegisterOnStatusChanged(s => statuses.Add(s));
        context.RegisterOnTurnAdded(_ => turnCount++);
        context.RegisterOnBlockAdded((_, _) => blockCount++);

        // Turn 1
        await context.SendMessageAsync("Hello");
        Assert.Single(context.Turns);
        Assert.NotEmpty(context.Turns[0].ResponseBlocks);
        Assert.Equal(ConversationStatus.Idle, context.Status);

        // Turn 2
        await context.SendMessageAsync("Tell me more");
        Assert.Equal(2, context.Turns.Count);
        Assert.NotEmpty(context.Turns[1].ResponseBlocks);
        Assert.Equal(ConversationStatus.Idle, context.Status);

        // Notifications
        Assert.Equal(2, turnCount);
        Assert.True(blockCount >= 4); // at least 2 user + 2 assistant blocks
        // Statuses: Streaming, Idle, Streaming, Idle
        Assert.Equal(4, statuses.Count);
    }
}

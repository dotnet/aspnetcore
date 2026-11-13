// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextThreadTests
{
    [Fact]
    public async Task RestoreAsync_RecreatesTurnsWithoutFiringCallbacks()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        var callCount = 0;
        client.SetHandler((_, _, _) =>
            ResponseEmitters.EmitTextResponse($"Response {++callCount}"));
        var firstContext = new AgentContext(CreateAgent(client, thread));
        await firstContext.SendMessageAsync("First");
        await firstContext.SendMessageAsync("Second");

        var restoredContext = new AgentContext(CreateAgent(client, thread));
        var turnAddedCount = 0;
        var blockAddedCount = 0;
        var statusChangedCount = 0;
        using var turnRegistration =
            restoredContext.RegisterOnTurnAdded(_ => turnAddedCount++);
        using var blockRegistration =
            restoredContext.RegisterOnBlockAdded((_, _) => blockAddedCount++);
        using var statusRegistration =
            restoredContext.RegisterOnStatusChanged(_ => statusChangedCount++);

        await restoredContext.RestoreAsync();

        Assert.Equal(2, restoredContext.Turns.Count);
        Assert.Equal("First", GetRequestText(restoredContext.Turns[0]));
        Assert.Equal("Response 1", GetResponseText(restoredContext.Turns[0]));
        Assert.Equal("Second", GetRequestText(restoredContext.Turns[1]));
        Assert.Equal("Response 2", GetResponseText(restoredContext.Turns[1]));
        Assert.Equal(0, turnAddedCount);
        Assert.Equal(0, blockAddedCount);
        Assert.Equal(0, statusChangedCount);
    }

    [Fact]
    public async Task RestoreAsync_EmptyThread_RemainsIdle()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        var context = new AgentContext(CreateAgent(client, thread));

        await context.RestoreAsync();

        Assert.Empty(context.Turns);
        Assert.Equal(ConversationStatus.Idle, context.Status);
    }

    private static UIAgent CreateAgent(
        IChatClient client,
        IConversationThread thread)
        => new(client, options => options.Thread = thread);

    private static string GetRequestText(ConversationTurn turn)
        => Assert.IsType<RichContentBlock>(Assert.Single(turn.RequestBlocks)).RawText;

    private static string GetResponseText(ConversationTurn turn)
        => Assert.IsType<RichContentBlock>(Assert.Single(turn.ResponseBlocks)).RawText;
}

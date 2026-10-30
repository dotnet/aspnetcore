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

    [Fact]
    public async Task RestoreAsync_PreservesMultimodalMessageBoundaries()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        var callCount = 0;
        client.SetHandler((_, _, _) =>
            ResponseEmitters.EmitTextResponse($"Response {++callCount}"));
        var firstContext = new AgentContext(CreateAgent(client, thread));
        var firstImage = new DataContent(new byte[] { 1, 2, 3 }, "image/png");
        var secondImage = new DataContent(new byte[] { 4, 5, 6 }, "image/png");
        await firstContext.SendMessageAsync(new ChatMessage(
            ChatRole.User,
            [new TextContent("First"), firstImage, secondImage]));
        await firstContext.SendMessageAsync(new ChatMessage(
            ChatRole.User,
            [new TextContent("Second"), firstImage]));

        var restoredContext = new AgentContext(CreateAgent(client, thread));
        await restoredContext.RestoreAsync();

        Assert.Equal(2, restoredContext.Turns.Count);
        for (var i = 0; i < firstContext.Turns.Count; i++)
        {
            var original = firstContext.Turns[i];
            var restored = restoredContext.Turns[i];
            Assert.Equal(original.RequestBlocks.Count, restored.RequestBlocks.Count);
            Assert.Equal(
                original.RequestBlocks.Select(block => block.GetType()),
                restored.RequestBlocks.Select(block => block.GetType()));
            Assert.Equal(
                original.RequestBlocks.OfType<RichContentBlock>().Select(block => block.RawText),
                restored.RequestBlocks.OfType<RichContentBlock>().Select(block => block.RawText));
            Assert.Equal(GetResponseText(original), GetResponseText(restored));
        }
    }

    [Fact]
    public async Task RestoreAsync_UserMessageWithoutBlocks_PreservesResponseBoundary()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        var callCount = 0;
        client.SetHandler((_, _, _) =>
            ResponseEmitters.EmitTextResponse($"Response {++callCount}"));
        var firstContext = new AgentContext(CreateAgent(client, thread));
        await firstContext.SendMessageAsync("First");
        await firstContext.SendMessageAsync(new ChatMessage(ChatRole.User, Array.Empty<AIContent>()));

        var restoredContext = new AgentContext(CreateAgent(client, thread));
        await restoredContext.RestoreAsync();

        Assert.Equal(2, restoredContext.Turns.Count);
        Assert.Equal("First", GetRequestText(restoredContext.Turns[0]));
        Assert.Equal("Response 1", GetResponseText(restoredContext.Turns[0]));
        Assert.Empty(restoredContext.Turns[1].RequestBlocks);
        Assert.Equal("Response 2", GetResponseText(restoredContext.Turns[1]));
    }

    [Fact]
    public async Task RestoreAsync_ConsecutiveUserMessagesRemainSeparateTurns()
    {
        var thread = new InMemoryConversationThread("thread-1");
        thread.AppendUserMessage(new ChatMessage(
            ChatRole.User,
            [new TextContent("First"), new DataContent(new byte[] { 1 }, "image/png")]));
        thread.CompleteTurn();
        thread.AppendUserMessage(new ChatMessage(
            ChatRole.User,
            [new TextContent("Second"), new DataContent(new byte[] { 2 }, "image/png")]));
        thread.CompleteTurn();
        var context = new AgentContext(CreateAgent(new DelegatingStreamingChatClient(), thread));

        await context.RestoreAsync();

        Assert.Equal(2, context.Turns.Count);
        Assert.All(context.Turns, turn =>
        {
            Assert.Equal(2, turn.RequestBlocks.Count);
            Assert.Empty(turn.ResponseBlocks);
        });
        Assert.Equal("First", Assert.Single(context.Turns[0].RequestBlocks.OfType<RichContentBlock>()).RawText);
        Assert.Equal("Second", Assert.Single(context.Turns[1].RequestBlocks.OfType<RichContentBlock>()).RawText);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentThreadTests
{
    [Fact]
    public async Task SendMessageAsync_WithThread_CommitsUserAndAssistantUpdates()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            ResponseEmitters.EmitMultiTokenTextResponse(cancellationToken, "A", "B", "C"));
        var agent = CreateAgent(client, thread);

        await CollectAsync(agent, "Hello");

        var updates = thread.GetUpdates();
        Assert.Equal(4, updates.Count);
        Assert.Equal(ChatRole.User, updates[0].Role);
        Assert.All(updates.Skip(1), update => Assert.Equal(ChatRole.Assistant, update.Role));
    }

    [Fact]
    public async Task SendMessageAsync_FailedStream_DoesNotWriteToThread()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, _) =>
            ResponseEmitters.EmitErrorAfterTokens(
                ["partial"],
                new InvalidOperationException("boom")));
        var agent = CreateAgent(client, thread);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(agent, "Hello"));

        Assert.Empty(thread.GetUpdates());
        Assert.False(thread.HasPendingTurn);
    }

    [Fact]
    public async Task SendMessageAsync_StatefulThread_ForwardsConversationIdWithoutReplayingHistory()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        ChatOptions? secondOptions = null;
        List<ChatMessage>? secondMessages = null;
        var callCount = 0;
        client.SetHandler((messages, options, cancellationToken) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitConversationId("conversation-1", cancellationToken);
            }

            secondMessages = messages.ToList();
            secondOptions = options;
            return ResponseEmitters.EmitTextResponse("Second response");
        });
        var agent = CreateAgent(client, thread);

        await CollectAsync(agent, "First");
        await CollectAsync(agent, "Second");

        Assert.Equal("conversation-1", secondOptions?.ConversationId);
        Assert.Equal(["Second"], secondMessages?.Select(message => message.Text));
    }

    [Fact]
    public async Task RestoreAsync_RestoresBlocksAndTypedState()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) => EmitState(cancellationToken));
        var firstAgent = CreateStateAgent(client, thread);

        await CollectAsync(firstAgent, "Create state");

        var restoredAgent = CreateStateAgent(client, thread);
        var blocks = await restoredAgent.RestoreAsync();

        Assert.Equal("Updated", restoredAgent.State.Value.Name);
        Assert.Equal(
            ["Create state", "State updated"],
            blocks.OfType<RichContentBlock>().Select(block => block.RawText));
    }

    [Fact]
    public async Task RestoreAsync_ThenSendMessage_IncludesRestoredHistory()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, _) => ResponseEmitters.EmitTextResponse("First response"));
        var firstAgent = CreateAgent(client, thread);
        await CollectAsync(firstAgent, "First");

        List<ChatMessage>? observedMessages = null;
        client.SetHandler((messages, _, _) =>
        {
            observedMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("Second response");
        });
        var restoredAgent = CreateAgent(client, thread);
        await restoredAgent.RestoreAsync();

        await CollectAsync(restoredAgent, "Second");

        Assert.Equal(
            ["First", "First response", "Second"],
            observedMessages?.Select(message => message.Text));
    }

    private static UIAgent CreateAgent(
        IChatClient client,
        IConversationThread thread)
        => new(client, options => options.Thread = thread);

    private static UIAgent<TestState> CreateStateAgent(
        IChatClient client,
        IConversationThread thread)
        => new(client, options =>
        {
            options.Thread = thread;
            options.StateMapper = context =>
            {
                var state = context.UnhandledContents.OfType<TestStateContent>().SingleOrDefault();
                if (state is null)
                {
                    return;
                }

                context.MarkHandled(state);
                context.SetState(state.Value);
            };
        });

    private static async Task<List<ContentBlock>> CollectAsync(UIAgent agent, string text)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(new ChatMessage(ChatRole.User, text)))
        {
            blocks.Add(block);
        }

        return blocks;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitConversationId(
        string conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            ConversationId = conversationId,
            Contents = [new TextContent("First response")],
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitState(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents =
            [
                new TestStateContent
                {
                    Value = new TestState { Name = "Updated" },
                },
                new TextContent("State updated"),
            ],
        };
        await Task.CompletedTask;
    }

    private sealed class TestState
    {
        public string Name { get; set; } = "";
    }

    private sealed class TestStateContent : AIContent
    {
        public required TestState Value { get; init; }
    }
}

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
    public async Task SendMessageAsync_FailedStream_DoesNotCommitTurn()
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
    }

    [Fact]
    public async Task SendMessageAsync_StatefulThread_ForwardsConversationId()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        ChatOptions? secondOptions = null;
        var callCount = 0;
        client.SetHandler((_, options, cancellationToken) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitConversationId("conversation-1", cancellationToken);
            }

            secondOptions = options;
            return ResponseEmitters.EmitTextResponse("Second response");
        });
        var agent = CreateAgent(client, thread);

        await CollectAsync(agent, "First");
        await CollectAsync(agent, "Second");

        Assert.Equal("conversation-1", secondOptions?.ConversationId);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoreAsync_FailureOrCancellation_DoesNotChangeHistoryOrState(
        bool cancel)
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            ResponseEmitters.EmitTextResponse("Current response", cancellationToken));
        CancellationTokenSource? restoreCts = null;
        var agent = new UIAgent<TestState>(client, options =>
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
                if (state.Value.Name == "Cancel")
                {
                    restoreCts!.Cancel();
                }
                else if (state.Value.Name == "Throw")
                {
                    throw new InvalidOperationException("Restore failed.");
                }
            };
        });
        await CollectAsync(agent, "Current request");
        agent.State.Value = new TestState { Name = "Current" };

        thread.Clear();
        CommitTurn(
            thread,
            new ChatMessage(ChatRole.User, "Restored request"),
            CreateStateUpdate(cancel ? "Cancel" : "Restored"),
            CreateStateUpdate(cancel ? "Unused" : "Throw"));

        if (cancel)
        {
            using var cancellationSource = new CancellationTokenSource();
            restoreCts = cancellationSource;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => agent.RestoreAsync(cancellationSource.Token));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => agent.RestoreAsync());
        }

        Assert.Equal("Current", agent.State.Value.Name);

        List<ChatMessage>? observedMessages = null;
        client.SetHandler((messages, _, cancellationToken) =>
        {
            observedMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("After response", cancellationToken);
        });

        await CollectAsync(agent, "After failure");

        Assert.Equal(
            ["Current request", "Current response", "After failure"],
            observedMessages?.Select(message => message.Text));
    }

    [Fact]
    public async Task RestoreAsync_EmptyThread_ClearsHistoryAndState()
    {
        var thread = new InMemoryConversationThread("thread-1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            ResponseEmitters.EmitTextResponse("Current response", cancellationToken));
        var agent = CreateStateAgent(client, thread);
        await CollectAsync(agent, "Current request");
        agent.State.Value = new TestState { Name = "Current" };
        thread.Clear();

        await agent.RestoreAsync();

        Assert.Equal("", agent.State.Value.Name);

        List<ChatMessage>? observedMessages = null;
        client.SetHandler((messages, _, cancellationToken) =>
        {
            observedMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("New response", cancellationToken);
        });

        await CollectAsync(agent, "New request");

        var message = Assert.Single(observedMessages!);
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("New request", message.Text);
    }

    [Fact]
    public async Task RestoreAsync_ToolMessage_PreservesRoleInHistory()
    {
        var thread = new InMemoryConversationThread("thread-1");
        CommitTurn(
            thread,
            new ChatMessage(ChatRole.User, "Initial request"),
            new ChatResponseUpdate(ChatRole.Assistant, "Tool requested"));
        CommitTurn(
            thread,
            new ChatMessage(ChatRole.Tool, "Tool result"),
            new ChatResponseUpdate(ChatRole.Assistant, "Final response"));
        var client = new DelegatingStreamingChatClient();
        List<ChatMessage>? observedMessages = null;
        client.SetHandler((messages, _, cancellationToken) =>
        {
            observedMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("Next response", cancellationToken);
        });
        var agent = CreateAgent(client, thread);

        var restoredBlocks = await agent.RestoreAsync();
        await CollectAsync(agent, "Next request");

        Assert.Equal(
            [
                ChatRole.User,
                ChatRole.Assistant,
                ChatRole.Tool,
                ChatRole.Assistant,
            ],
            restoredBlocks.OfType<RichContentBlock>().Select(block => block.Role));
        Assert.Equal(
            ["Initial request", "Tool requested", "Tool result", "Final response"],
            restoredBlocks.OfType<RichContentBlock>().Select(block => block.RawText));
        Assert.Equal(
            [
                ChatRole.User,
                ChatRole.Assistant,
                ChatRole.Tool,
                ChatRole.Assistant,
                ChatRole.User,
            ],
            observedMessages?.Select(message => message.Role));
    }

    [Fact]
    public async Task RestoreAsync_ResetsTypedStateBeforeReplayingUpdates()
    {
        var thread = new InMemoryConversationThread("thread-1");
        CommitTurn(
            thread,
            new ChatMessage(ChatRole.User, "Restore"),
            new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TestStateDeltaContent { Suffix = "Restored" }],
            });
        var client = new DelegatingStreamingChatClient();
        UIAgent<TestState> agent = null!;
        agent = new UIAgent<TestState>(client, options =>
        {
            options.Thread = thread;
            options.StateMapper = context =>
            {
                var delta = context.UnhandledContents
                    .OfType<TestStateDeltaContent>()
                    .SingleOrDefault();
                if (delta is null)
                {
                    return;
                }

                context.MarkHandled(delta);
                context.SetState(new TestState
                {
                    Name = agent.State.Value.Name + delta.Suffix,
                });
            };
        });
        agent.State.Value = new TestState { Name = "Stale" };

        await agent.RestoreAsync();

        Assert.Equal("Restored", agent.State.Value.Name);
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

    private static void CommitTurn(
        InMemoryConversationThread thread,
        ChatMessage message,
        params ChatResponseUpdate[] updates)
    {
        thread.AppendMessages([message]);
        foreach (var update in updates)
        {
            thread.AppendUpdate(update);
        }
        thread.CompleteTurn();
    }

    private static ChatResponseUpdate CreateStateUpdate(string name)
        => new()
        {
            Role = ChatRole.Assistant,
            Contents =
            [
                new TestStateContent
                {
                    Value = new TestState { Name = name },
                },
            ],
        };

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

    private sealed class TestStateDeltaContent : AIContent
    {
        public required string Suffix { get; init; }
    }
}

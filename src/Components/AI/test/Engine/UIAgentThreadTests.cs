// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentThreadTests
{
    [Fact]
    public async Task SendMessage_WithThread_AppendsUserMessage()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi!"));

        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        await foreach (var _ in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hello")))
        {
        }

        var updates = thread.GetUpdates();
        Assert.True(updates.Count >= 1);
        Assert.Equal(ChatRole.User, updates[0].Role);
        var text = Assert.IsType<TextContent>(updates[0].Contents[0]);
        Assert.Equal("Hello", text.Text);
    }

    [Fact]
    public async Task SendMessage_WithThread_AppendsAllUpdates()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitMultiTokenTextResponse(ct, "A", "B", "C"));

        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        await foreach (var _ in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hi")))
        {
        }

        var updates = thread.GetUpdates();
        // 1 user + 3 assistant tokens
        Assert.Equal(4, updates.Count);
        Assert.Equal(ChatRole.User, updates[0].Role);
        Assert.Equal(ChatRole.Assistant, updates[1].Role);
        Assert.Equal(ChatRole.Assistant, updates[2].Role);
        Assert.Equal(ChatRole.Assistant, updates[3].Role);
    }

    [Fact]
    public async Task SendMessage_WithThread_CallsCompleteTurn()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));

        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        await foreach (var _ in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "go")))
        {
        }

        // CompleteTurn was called, so updates should be visible
        Assert.NotEmpty(thread.GetUpdates());
    }

    [Fact]
    public async Task SendMessage_WithThread_FailedStream_DoesNotCommitTurn()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitErrorAfterTokens(["partial"], new InvalidOperationException("boom")));

        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        var blocks = new List<ContentBlock>();
        try
        {
            await foreach (var block in agent.SendMessageAsync(
                new ChatMessage(ChatRole.User, "go")))
            {
                blocks.Add(block);
            }
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // CompleteTurn was NOT called because the stream threw
        Assert.Empty(thread.GetUpdates());
    }

    [Fact]
    public async Task SendMessage_WithStatefulThread_SetsConversationId()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        ChatOptions? capturedOptions = null;
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitWithConversationId("conv-123", ct);
            }
            capturedOptions = opts;
            return ResponseEmitters.EmitTextResponse("reply2");
        });

        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        // First call — LLM returns ConversationId
        await foreach (var _ in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "First")))
        {
        }

        Assert.True(thread.IsStateful);
        Assert.Equal("conv-123", thread.ConversationId);

        // Second call — ConversationId should be set on ChatOptions
        await foreach (var _ in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Second")))
        {
        }

        Assert.NotNull(capturedOptions);
        Assert.Equal("conv-123", capturedOptions!.ConversationId);
    }

    [Fact]
    public async Task SendMessage_WithoutThread_WorksAsNormal()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi!"));

        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hello")))
        {
            blocks.Add(block);
        }

        Assert.True(blocks.Count >= 2);
    }

    [Fact]
    public async Task RestoreAsync_ProducesCorrectBlocks()
    {
        // Step 1: Record a multi-turn conversation
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            return ResponseEmitters.EmitTextResponse($"Reply {callCount}");
        });

        var agent1 = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        await foreach (var _ in agent1.SendMessageAsync(
            new ChatMessage(ChatRole.User, "First")))
        {
        }
        await foreach (var _ in agent1.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Second")))
        {
        }

        Assert.NotEmpty(thread.GetUpdates());

        // Step 2: Restore on a fresh agent with same thread
        var agent2 = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        var restoredBlocks = await agent2.RestoreAsync();

        // Should have user blocks and assistant blocks for both turns
        var userBlocks = restoredBlocks.Where(b => b.Role == ChatRole.User).ToList();
        var assistantBlocks = restoredBlocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.Equal(2, userBlocks.Count);
        Assert.Equal(2, assistantBlocks.Count);

        // Verify user messages
        var user1 = Assert.IsType<RichContentBlock>(userBlocks[0]);
        Assert.Equal("First", user1.RawText);
        var user2 = Assert.IsType<RichContentBlock>(userBlocks[1]);
        Assert.Equal("Second", user2.RawText);

        // Verify assistant responses
        var asst1 = Assert.IsType<RichContentBlock>(assistantBlocks[0]);
        Assert.Equal("Reply 1", asst1.RawText);
        var asst2 = Assert.IsType<RichContentBlock>(assistantBlocks[1]);
        Assert.Equal("Reply 2", asst2.RawText);
    }

    [Fact]
    public async Task RestoreAsync_RestoresState()
    {
        // Record a conversation with state content
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => EmitStateAndText(ct));

        var agent1 = new UIAgent<TestState>(client,
            configure: options =>
            {
                options.Thread = thread;
                options.StateMapper = ctx =>
                {
                    foreach (var content in ctx.UnhandledContents)
                    {
                        if (content is TestStateContent sc)
                        {
                            ctx.MarkHandled(sc);
                            ctx.SetState(sc.StateValue);
                            return true;
                        }
                    }
                    return false;
                };
            });

        await foreach (var _ in agent1.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Give me state")))
        {
        }

        Assert.Equal("Updated", agent1.State.Value.Name);

        // Restore on fresh agent
        var agent2 = new UIAgent<TestState>(client,
            configure: options =>
            {
                options.Thread = thread;
                options.StateMapper = ctx =>
                {
                    foreach (var content in ctx.UnhandledContents)
                    {
                        if (content is TestStateContent sc)
                        {
                            ctx.MarkHandled(sc);
                            ctx.SetState(sc.StateValue);
                            return true;
                        }
                    }
                    return false;
                };
            });

        await agent2.RestoreAsync();

        Assert.Equal("Updated", agent2.State.Value.Name);
    }

    [Fact]
    public async Task RestoreAsync_ThenSendMessage_ContinuesConversation()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            return ResponseEmitters.EmitTextResponse($"Reply {callCount}");
        });

        // Record 1 turn
        var agent1 = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        await foreach (var _ in agent1.SendMessageAsync(
            new ChatMessage(ChatRole.User, "First")))
        {
        }

        // Restore and then send a new message
        var agent2 = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        await agent2.RestoreAsync();

        // New message after restore
        IEnumerable<ChatMessage>? capturedMessages = null;
        client.SetHandler((msgs, opts, ct) =>
        {
            capturedMessages = msgs.ToList();
            return ResponseEmitters.EmitTextResponse("Reply after restore");
        });

        await foreach (var _ in agent2.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Second")))
        {
        }

        // The history sent to the LLM should include both the restored turn and the new message
        Assert.NotNull(capturedMessages);
        var messageList = capturedMessages!.ToList();
        Assert.True(messageList.Count >= 3, $"Expected at least 3 messages, got {messageList.Count}");
    }

    [Fact]
    public async Task RestoreAsync_NoThread_ReturnsEmpty()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var blocks = await agent.RestoreAsync();

        Assert.Empty(blocks);
    }

    [Fact]
    public async Task RestoreAsync_EmptyThread_ReturnsEmpty()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        var blocks = await agent.RestoreAsync();

        Assert.Empty(blocks);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitWithConversationId(
        string conversationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            ConversationId = conversationId,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent("reply1")]
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitStateAndText(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TestStateContent
            {
                StateValue = new TestState { Name = "Updated" }
            }]
        };
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("Here's the result")]
        };
        await Task.CompletedTask;
    }

    internal class TestState
    {
        public string Name { get; set; } = "";
    }

    internal class TestStateContent : AIContent
    {
        public TestState StateValue { get; set; } = new();
    }
}

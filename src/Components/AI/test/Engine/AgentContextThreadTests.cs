// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextThreadTests
{
    [Fact]
    public async Task RestoreAsync_CreatesTurns()
    {
        var (agent, client, thread) = CreateAgentWithThread();

        // Record 2 turns
        client.SetHandler(CreateCountingHandler());
        var context1 = new AgentContext(agent);
        await context1.SendMessageAsync("First");
        await context1.SendMessageAsync("Second");

        Assert.NotEmpty(thread.GetUpdates());

        // Restore on fresh agent
        var (agent2, _, _) = CreateAgentWithExistingThread(thread, client);
        var context2 = new AgentContext(agent2);
        await context2.RestoreAsync();

        Assert.Equal(2, context2.Turns.Count);
    }

    [Fact]
    public async Task RestoreAsync_DoesNotFireCallbacks()
    {
        var (agent, client, thread) = CreateAgentWithThread();

        // Record 2 turns
        client.SetHandler(CreateCountingHandler());
        var context1 = new AgentContext(agent);
        await context1.SendMessageAsync("First");
        await context1.SendMessageAsync("Second");

        // Restore on fresh agent with callbacks
        var (agent2, _, _) = CreateAgentWithExistingThread(thread, client);
        var context2 = new AgentContext(agent2);

        var turnAddedCount = 0;
        var blockAddedCount = 0;
        var statusChangedCount = 0;
        context2.RegisterOnTurnAdded(_ => turnAddedCount++);
        context2.RegisterOnBlockAdded((_, _) => blockAddedCount++);
        context2.RegisterOnStatusChanged(_ => statusChangedCount++);

        await context2.RestoreAsync();

        // RestoreAsync populates turns without firing callbacks
        Assert.Equal(2, context2.Turns.Count);
        Assert.Equal(0, turnAddedCount);
        Assert.Equal(0, blockAddedCount);
        Assert.Equal(0, statusChangedCount);
    }

    [Fact]
    public async Task RestoreAsync_PopulatesRequestAndResponseBlocks()
    {
        var (agent, client, thread) = CreateAgentWithThread();

        // Record 1 turn
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi!"));
        var context1 = new AgentContext(agent);
        await context1.SendMessageAsync("Hello");

        // Restore on fresh agent
        var (agent2, _, _) = CreateAgentWithExistingThread(thread, client);
        var context2 = new AgentContext(agent2);

        await context2.RestoreAsync();

        Assert.Single(context2.Turns);
        var turn = context2.Turns[0];
        Assert.NotEmpty(turn.RequestBlocks);
        Assert.NotEmpty(turn.ResponseBlocks);
    }

    [Fact]
    public async Task RestoreAsync_StatusRemainsIdle()
    {
        var (agent, client, thread) = CreateAgentWithThread();

        // Record 1 turn
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context1 = new AgentContext(agent);
        await context1.SendMessageAsync("hi");

        // Restore with status tracking
        var (agent2, _, _) = CreateAgentWithExistingThread(thread, client);
        var context2 = new AgentContext(agent2);

        await context2.RestoreAsync();

        Assert.Equal(ConversationStatus.Idle, context2.Status);
    }

    [Fact]
    public async Task RestoreAsync_ThenSendMessage_AppendsNewTurn()
    {
        var (agent, client, thread) = CreateAgentWithThread();

        // Record 1 turn
        client.SetHandler(CreateCountingHandler());
        var context1 = new AgentContext(agent);
        await context1.SendMessageAsync("First");

        // Restore then send new message
        var (agent2, client2, _) = CreateAgentWithExistingThread(thread, client);
        var context2 = new AgentContext(agent2);
        await context2.RestoreAsync();

        Assert.Single(context2.Turns);

        client2.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("New reply"));

        await context2.SendMessageAsync("Second");

        Assert.Equal(2, context2.Turns.Count);
    }

    [Fact]
    public async Task RestoreAsync_EmptyThread_RemainsIdle()
    {
        var thread = new InMemoryConversationThread("t1");
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });

        var context = new AgentContext(agent);
        await context.RestoreAsync();

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Empty(context.Turns);
    }

    [Fact]
    public async Task RestoreAsync_RequestBlocksAreUserRole_ResponseBlocksAreAssistant()
    {
        var (agent, client, thread) = CreateAgentWithThread();

        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Reply"));
        var context1 = new AgentContext(agent);
        await context1.SendMessageAsync("Question");

        var (agent2, _, _) = CreateAgentWithExistingThread(thread, client);
        var context2 = new AgentContext(agent2);
        await context2.RestoreAsync();

        var turn = context2.Turns[0];
        Assert.All(turn.RequestBlocks, b => Assert.Equal(ChatRole.User, b.Role));
        Assert.All(turn.ResponseBlocks, b => Assert.Equal(ChatRole.Assistant, b.Role));
    }

    private static (UIAgent agent, DelegatingStreamingChatClient client, InMemoryConversationThread thread) CreateAgentWithThread()
    {
        var thread = new InMemoryConversationThread(Guid.NewGuid().ToString("N"));
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client, options =>
        {
            options.Thread = thread;
        });
        return (agent, client, thread);
    }

    private static (UIAgent agent, DelegatingStreamingChatClient client, InMemoryConversationThread thread) CreateAgentWithExistingThread(
        InMemoryConversationThread thread,
        DelegatingStreamingChatClient existingClient)
    {
        var agent = new UIAgent(existingClient, options =>
        {
            options.Thread = thread;
        });
        return (agent, existingClient, thread);
    }

    private static Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> CreateCountingHandler()
    {
        var count = 0;
        return (msgs, opts, ct) =>
        {
            count++;
            return ResponseEmitters.EmitTextResponse($"Reply {count}");
        };
    }
}

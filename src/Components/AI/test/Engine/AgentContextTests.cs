// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextTests
{
    private static (UIAgent agent, DelegatingStreamingChatClient client) CreateAgent()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);
        return (agent, client);
    }

    // ---- Turn Assembly ----

    [Fact]
    public async Task SendMessageAsync_CreatesTurnWithUserAndAssistantBlocks()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi!"));
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Hello");

        Assert.Single(context.Turns);
        var turn = context.Turns[0];
        Assert.NotEmpty(turn.RequestBlocks);
        Assert.All(turn.RequestBlocks, b => Assert.Equal(ChatRole.User, b.Role));
        Assert.NotEmpty(turn.ResponseBlocks);
        Assert.All(turn.ResponseBlocks, b => Assert.Equal(ChatRole.Assistant, b.Role));
    }

    [Fact]
    public async Task SendMessageAsync_MultipleCalls_CreatesMultipleTurns()
    {
        var (agent, client) = CreateAgent();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            return ResponseEmitters.EmitTextResponse($"Response {callCount}");
        });
        var context = new AgentContext(agent);

        await context.SendMessageAsync("First");
        await context.SendMessageAsync("Second");

        Assert.Equal(2, context.Turns.Count);
    }

    [Fact]
    public async Task SendMessageAsync_RequestBlocksContainUserText()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Reply"));
        var context = new AgentContext(agent);

        await context.SendMessageAsync("My message");

        var userBlock = Assert.IsType<RichContentBlock>(context.Turns[0].RequestBlocks[0]);
        Assert.Equal("My message", userBlock.RawText);
    }

    // ---- Status Transitions ----

    [Fact]
    public async Task Status_StartsAsIdle()
    {
        var (agent, _) = CreateAgent();
        var context = new AgentContext(agent);

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Null(context.Error);
    }

    [Fact]
    public async Task SendMessageAsync_StatusTransitions_IdleToStreamingToIdle()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s => statuses.Add(s));

        await context.SendMessageAsync("hi");

        Assert.Equal(new[] { ConversationStatus.Streaming, ConversationStatus.Idle }, statuses);
    }

    [Fact]
    public async Task SendMessageAsync_OnException_StatusIsError()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitErrorAfterTokens([], new InvalidOperationException("boom")));
        var context = new AgentContext(agent);

        await context.SendMessageAsync("hi");

        Assert.Equal(ConversationStatus.Error, context.Status);
        Assert.NotNull(context.Error);
        Assert.Equal("boom", context.Error!.Message);
    }

    [Fact]
    public async Task SendMessageAsync_OnException_StatusTransitions_IdleToStreamingToError()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitErrorAfterTokens(["partial"], new Exception("fail")));
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s => statuses.Add(s));

        await context.SendMessageAsync("hi");

        Assert.Equal(
            new[] { ConversationStatus.Streaming, ConversationStatus.Error },
            statuses);
    }

    // ---- Notifications ----

    [Fact]
    public async Task RegisterOnTurnAdded_FiresWhenTurnCreated()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context = new AgentContext(agent);

        ConversationTurn? receivedTurn = null;
        context.RegisterOnTurnAdded(t => receivedTurn = t);

        await context.SendMessageAsync("hi");

        Assert.NotNull(receivedTurn);
        Assert.Same(context.Turns[0], receivedTurn);
    }

    [Fact]
    public async Task RegisterOnBlockAdded_FiresForEachBlock()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context = new AgentContext(agent);

        var receivedBlocks = new List<(ConversationTurn turn, ContentBlock block)>();
        context.RegisterOnBlockAdded((t, b) => receivedBlocks.Add((t, b)));

        await context.SendMessageAsync("hi");

        Assert.True(receivedBlocks.Count >= 2);
        Assert.All(receivedBlocks, x => Assert.Same(context.Turns[0], x.turn));
    }

    [Fact]
    public async Task RegisterOnStatusChanged_DisposingStopsCallbacks()
    {
        var (agent, client) = CreateAgent();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            return ResponseEmitters.EmitTextResponse($"Reply {callCount}");
        });
        var context = new AgentContext(agent);

        var statusCount = 0;
        var reg = context.RegisterOnStatusChanged(_ => statusCount++);

        await context.SendMessageAsync("first");
        var countAfterFirst = statusCount;

        reg.Dispose();

        await context.SendMessageAsync("second");
        Assert.Equal(countAfterFirst, statusCount);
    }

    [Fact]
    public async Task MultipleRegistrations_AllReceiveCallbacks()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context = new AgentContext(agent);

        var count1 = 0;
        var count2 = 0;
        context.RegisterOnStatusChanged(_ => count1++);
        context.RegisterOnStatusChanged(_ => count2++);

        await context.SendMessageAsync("hi");

        Assert.Equal(count1, count2);
        Assert.True(count1 > 0);
    }

    // ---- Notification Ordering ----

    [Fact]
    public async Task Notifications_FireInCorrectOrder()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context = new AgentContext(agent);

        var events = new List<string>();
        context.RegisterOnStatusChanged(s => events.Add($"status:{s}"));
        context.RegisterOnTurnAdded(_ => events.Add("turn-added"));
        context.RegisterOnBlockAdded((_, b) => events.Add($"block:{b.Role}"));

        await context.SendMessageAsync("hi");

        Assert.Equal("turn-added", events[0]);
        Assert.Equal("status:Streaming", events[1]);
        Assert.Equal("status:Idle", events[^1]);

        var blockEvents = events.Where(e => e.StartsWith("block:", StringComparison.Ordinal)).ToList();
        Assert.True(blockEvents.Count >= 2);
    }

    // ---- String overload ----

    [Fact]
    public async Task SendMessageAsync_StringOverload_CreatesProperChatMessage()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("ok"));
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Hello world");

        var userBlock = Assert.IsType<RichContentBlock>(context.Turns[0].RequestBlocks[0]);
        Assert.Equal("Hello world", userBlock.RawText);
    }
}

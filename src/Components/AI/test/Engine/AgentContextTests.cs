// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextTests
{
    [Fact]
    public async Task SendMessageAsync_CreatesTurnWithRequestAndResponseBlocks()
    {
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse("Hi there!"));

        await context.SendMessageAsync("Hello");

        var turn = Assert.Single(context.Turns);
        var request = Assert.IsType<RichContentBlock>(Assert.Single(turn.RequestBlocks));
        Assert.Equal("Hello", request.RawText);
        var response = Assert.IsType<RichContentBlock>(Assert.Single(turn.ResponseBlocks));
        Assert.Equal("Hi there!", response.RawText);
    }

    [Fact]
    public async Task SendMessageAsync_TransitionsStreamingThenIdle()
    {
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse("Hi"));
        var statuses = new List<ConversationStatus>();
        using var subscription = context.RegisterOnStatusChanged(statuses.Add);

        await context.SendMessageAsync("Hello");

        Assert.Equal([ConversationStatus.Streaming, ConversationStatus.Idle], statuses);
        Assert.Equal(ConversationStatus.Idle, context.Status);
    }

    [Fact]
    public async Task SendMessageAsync_NotifiesTurnAndBlockSubscribers()
    {
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse("Hi"));
        var turnCount = 0;
        var blockCount = 0;
        using var turnSubscription = context.RegisterOnTurnAdded(_ => turnCount++);
        using var blockSubscription = context.RegisterOnBlockAdded((_, _) => blockCount++);

        await context.SendMessageAsync("Hello");

        Assert.Equal(1, turnCount);
        Assert.Equal(2, blockCount);
    }

    [Fact]
    public async Task RegisterOnStatusChanged_DisposedSubscriptionStopsReceivingUpdates()
    {
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse("Hi"));
        var statusCount = 0;
        var subscription = context.RegisterOnStatusChanged(_ => statusCount++);

        subscription.Dispose();
        await context.SendMessageAsync("Hello");

        Assert.Equal(0, statusCount);
    }

    [Fact]
    public async Task SendMessageAsync_SecondTurn_AppendsAnotherTurn()
    {
        var callCount = 0;
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse($"Answer {++callCount}"));

        await context.SendMessageAsync("First");
        await context.SendMessageAsync("Second");

        Assert.Equal(2, context.Turns.Count);
        Assert.Equal("Second", Assert.IsType<RichContentBlock>(context.Turns[1].RequestBlocks[0]).RawText);
        Assert.Equal("Answer 2", Assert.IsType<RichContentBlock>(context.Turns[1].ResponseBlocks[0]).RawText);
    }

    [Fact]
    public async Task SendMessageAsync_Failure_SurfacesErrorStatus()
    {
        var context = CreateContext(_ =>
            ResponseEmitters.EmitErrorAfterTokens(["partial"], new InvalidOperationException("boom")));

        await context.SendMessageAsync("Hello");

        Assert.Equal(ConversationStatus.Error, context.Status);
        Assert.IsType<InvalidOperationException>(context.Error);
    }

    [Fact]
    public async Task RetryAsync_RepeatedFailuresPreserveOneRequestAndNoPartialResponse()
    {
        var callCount = 0;
        var requestHistoryCounts = new List<int>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, _, _) =>
        {
            requestHistoryCounts.Add(messages.Count());
            return callCount++ < 2
                ? ResponseEmitters.EmitErrorAfterTokens(
                    ["partial"],
                    new InvalidOperationException("boom"))
                : ResponseEmitters.EmitTextResponse("Recovered");
        });
        var context = new AgentContext(new UIAgent(client));

        await context.SendMessageAsync("Hello");
        var turn = Assert.Single(context.Turns);
        Assert.Single(turn.RequestBlocks);
        Assert.Empty(turn.ResponseBlocks);

        await context.RetryAsync();
        Assert.Single(turn.RequestBlocks);
        Assert.Empty(turn.ResponseBlocks);

        await context.RetryAsync();

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Null(context.Error);
        Assert.Equal([1, 1, 1], requestHistoryCounts);
        Assert.Single(turn.RequestBlocks);
        Assert.Equal("Recovered", Assert.IsType<RichContentBlock>(Assert.Single(turn.ResponseBlocks)).RawText);
    }

    [Fact]
    public async Task RetryAsync_WithoutError_Throws()
    {
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse("Hi"));

        await context.SendMessageAsync("Hello");

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.RetryAsync());
    }

    [Fact]
    public async Task CancelAsync_DuringStreaming_ClearsResponseBlocksAndReturnsToIdle()
    {
        AgentContext? context = null;
        var gate = new TaskCompletionSource();
        context = CreateContext(ct => ResponseEmitters.EmitTokensWithGate(
            ["first", "second"],
            async index =>
            {
                if (index == 1)
                {
                    await gate.Task;
                }
            },
            ct));

        var sendTask = context.SendMessageAsync("Hello");
        var cancelTask = context.CancelAsync();
        gate.SetResult();
        await cancelTask;
        await sendTask;

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Empty(Assert.Single(context.Turns).ResponseBlocks);
    }

    [Fact]
    public async Task CancelAsync_WhenIdle_IsNoOp()
    {
        var context = CreateContext(_ => ResponseEmitters.EmitTextResponse("Hi"));

        await context.CancelAsync();

        Assert.Equal(ConversationStatus.Idle, context.Status);
    }

    [Fact]
    public void Constructor_NullAgentThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new AgentContext(null!));
    }

    private static AgentContext CreateContext(
        Func<CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> respond)
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => respond(ct));
        return new AgentContext(new UIAgent(client));
    }
}

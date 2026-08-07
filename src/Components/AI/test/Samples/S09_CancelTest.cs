// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S09_CancelTest
{
    // AG-UI cancellation maps to AgentContext.CancelAsync in components-ai.
    // Cancelling discards partial response blocks and returns to Idle.

    [Fact]
    public async Task CancelAsync_DiscardsPartialBlocks_ReturnsIdle()
    {
        var streamStarted = new TaskCompletionSource();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => SlowStream(streamStarted, ct));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        var sendTask = context.SendMessageAsync("Write a long essay");
        await streamStarted.Task;

        Assert.Equal(ConversationStatus.Streaming, context.Status);

        await context.CancelAsync();
        await sendTask;

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Null(context.Error);

        var turn = context.Turns[0];
        Assert.Empty(turn.ResponseBlocks);
    }

    [Fact]
    public async Task CancelAsync_WhenIdle_IsNoOp()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Done", ct));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        await context.SendMessageAsync("Hi");

        Assert.Equal(ConversationStatus.Idle, context.Status);
        await context.CancelAsync();
        Assert.Equal(ConversationStatus.Idle, context.Status);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> SlowStream(
        TaskCompletionSource streamStarted,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent("Once upon a time")]
        };
        streamStarted.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            yield break;
        }
    }
}

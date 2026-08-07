// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S08_ErrorRetryTest
{
    // AG-UI error recovery maps to AgentContext error/retry in components-ai.
    // An LLM failure mid-stream sets Error status; RetryAsync re-submits the same turn.

    [Fact]
    public async Task ErrorDuringStreaming_SetsErrorStatus_WithPartialBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitErrorAfterTokens(
                ["Partial", " response"],
                new InvalidOperationException("LLM rate limited"),
                ct));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Summarize this document");

        Assert.Equal(ConversationStatus.Error, context.Status);
        Assert.IsType<InvalidOperationException>(context.Error);
        Assert.Single(context.Turns);
        Assert.NotEmpty(context.Turns[0].ResponseBlocks);
    }

    [Fact]
    public async Task RetryAsync_ClearsErrorAndResubmits()
    {
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitErrorAfterTokens(
                    ["Partial"], new Exception("Connection lost"), ct);
            }
            return ResponseEmitters.EmitTextResponse("Full summary of the document.", ct);
        });

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Summarize");
        Assert.Equal(ConversationStatus.Error, context.Status);

        await context.RetryAsync();

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Null(context.Error);
        Assert.Single(context.Turns);

        var text = context.Turns[0].ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Equal("Full summary of the document.", text.RawText);
    }

    [Fact]
    public async Task RetryAsync_StatusTransitions_ErrorStreamingIdle()
    {
        var statuses = new List<ConversationStatus>();
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitErrorAfterTokens(
                    [], new Exception("fail"), ct);
            }
            return ResponseEmitters.EmitTextResponse("OK", ct);
        });

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(s => statuses.Add(s));

        await context.SendMessageAsync("Go");
        // Streaming → Error
        statuses.Clear();

        await context.RetryAsync();
        // Should see Streaming → Idle
        Assert.Equal(ConversationStatus.Streaming, statuses[0]);
        Assert.Equal(ConversationStatus.Idle, statuses[^1]);
    }
}

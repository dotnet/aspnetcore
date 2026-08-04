// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentReasoningTests
{
    [Fact]
    public async Task SendMessageAsync_ReasoningThenText_YieldsBothBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitReasoningThenTextResponse(
                "Let me think...", "The answer is 42."));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "What is the meaning of life?")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.Equal(2, assistantBlocks.Count);

        var reasoning = Assert.IsType<ReasoningContentBlock>(assistantBlocks[0]);
        Assert.Equal("Let me think...", reasoning.Text);
        Assert.Equal(BlockLifecycleState.Inactive, reasoning.LifecycleState);

        var text = Assert.IsType<RichContentBlock>(assistantBlocks[1]);
        Assert.Equal("The answer is 42.", text.RawText);
        Assert.Equal(BlockLifecycleState.Inactive, text.LifecycleState);
    }

    [Fact]
    public async Task SendMessageAsync_ReasoningOnly_YieldsReasoningBlock()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => YieldReasoningOnly(ct));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Think about this")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.Single(assistantBlocks);
        var reasoning = Assert.IsType<ReasoningContentBlock>(assistantBlocks[0]);
        Assert.Equal("Deep thoughts...", reasoning.Text);
        Assert.Equal(BlockLifecycleState.Inactive, reasoning.LifecycleState);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldReasoningOnly(
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("Deep thoughts...")]
        };
        await Task.CompletedTask;
    }
}

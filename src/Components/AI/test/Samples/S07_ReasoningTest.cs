// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S07_ReasoningTest
{
    // AG-UI "thought" / reasoning content maps to ReasoningContentBlock in components-ai.
    // The LLM emits TextReasoningContent before the visible text.

    [Fact]
    public async Task ReasoningThenText_ProducesBothBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitReasoningThenTextResponse(
                "Let me think about this step by step...",
                "The answer is 42.",
                ct));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("What is the meaning of life?");

        var turn = context.Turns[0];
        var reasoning = turn.ResponseBlocks.OfType<ReasoningContentBlock>().Single();
        Assert.Equal("Let me think about this step by step...", reasoning.Text);

        var text = turn.ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Equal("The answer is 42.", text.RawText);
    }

    [Fact]
    public async Task ReasoningBlock_IsBeforeTextBlock_InOrder()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitReasoningThenTextResponse("Thinking...", "Done.", ct));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        await context.SendMessageAsync("Think");

        var turn = context.Turns[0];
        var blocks = turn.ResponseBlocks.ToList();

        var reasoningIdx = blocks.FindIndex(b => b is ReasoningContentBlock);
        var textIdx = blocks.FindIndex(b => b is RichContentBlock);

        Assert.True(reasoningIdx < textIdx,
            "Reasoning block should appear before text block.");
    }
}

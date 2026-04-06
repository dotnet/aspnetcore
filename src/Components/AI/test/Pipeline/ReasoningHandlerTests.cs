// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class ReasoningHandlerTests
{
    private static BlockMappingPipeline CreatePipeline()
    {
        var options = new UIAgentOptions();
        return new BlockMappingPipeline(options);
    }

    private static async Task<List<ContentBlock>> ProcessAsync(
        BlockMappingPipeline pipeline, ChatResponseUpdate update)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in pipeline.Process(update))
        {
            blocks.Add(block);
        }
        return blocks;
    }

    [Fact]
    public async Task TextReasoningContent_EmitsReasoningContentBlock()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("Thinking about this...")]
        };

        var blocks = await ProcessAsync(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<ReasoningContentBlock>(blocks[0]);
        Assert.Equal("Thinking about this...", block.Text);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
    }

    [Fact]
    public async Task MultipleReasoningTokens_SingleBlockAccumulates()
    {
        var pipeline = CreatePipeline();

        var update1 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("Step 1: ")]
        };
        var update2 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("analyze the problem")]
        };

        var blocks1 = await ProcessAsync(pipeline, update1);
        Assert.Single(blocks1);
        await ProcessAsync(pipeline, update2);

        var block = Assert.IsType<ReasoningContentBlock>(blocks1[0]);
        Assert.Equal("Step 1: analyze the problem", block.Text);
    }

    [Fact]
    public async Task ReasoningFollowedByText_CompletesReasoningEmitsText()
    {
        var pipeline = CreatePipeline();

        var reasoningUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("Thinking...")]
        };
        var reasoningBlocks = await ProcessAsync(pipeline, reasoningUpdate);

        var textUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("The answer is 42.")]
        };
        var textBlocks = await ProcessAsync(pipeline, textUpdate);

        var reasoningBlock = Assert.IsType<ReasoningContentBlock>(reasoningBlocks[0]);
        Assert.Equal(BlockLifecycleState.Inactive, reasoningBlock.LifecycleState);

        Assert.Single(textBlocks);
        var textBlock = Assert.IsType<RichContentBlock>(textBlocks[0]);
        Assert.Equal("The answer is 42.", textBlock.RawText);
    }

    [Fact]
    public async Task ProtectedData_SetsIsEncrypted()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent(null) { ProtectedData = "encrypted-data" }]
        };

        var blocks = await ProcessAsync(pipeline, update);
        var block = Assert.IsType<ReasoningContentBlock>(blocks[0]);
        Assert.True(block.IsEncrypted);
        Assert.Equal("encrypted-data", block.ProtectedData);
    }

    [Fact]
    public async Task OnChanged_FiresOnSubsequentReasoningTokens()
    {
        var pipeline = CreatePipeline();

        var update1 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("A")]
        };
        var blocks = await ProcessAsync(pipeline, update1);
        var block = blocks[0];
        var changeCount = 0;
        block.OnChanged(() => changeCount++);

        var update2 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextReasoningContent("B")]
        };
        await ProcessAsync(pipeline, update2);

        Assert.Equal(1, changeCount);
    }
}

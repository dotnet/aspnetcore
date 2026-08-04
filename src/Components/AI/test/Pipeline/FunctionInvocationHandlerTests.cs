// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class FunctionInvocationHandlerTests
{
    private static BlockMappingPipeline CreatePipeline()
    {
        var options = new UIAgentOptions();
        return new BlockMappingPipeline(options);
    }

    private static async Task<List<ContentBlock>> CollectBlocks(
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
    public async Task FunctionCallContent_EmitsFunctionInvocationContentBlock()
    {
        var pipeline = CreatePipeline();
        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "GetWeather",
                new Dictionary<string, object?> { ["city"] = "Seattle" })],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<FunctionInvocationContentBlock>(blocks[0]);
        Assert.Equal("GetWeather", block.ToolName);
        Assert.Equal("call-1", block.Id);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
        Assert.False(block.HasResult);
    }

    [Fact]
    public async Task FunctionResultContent_MatchingCallId_CompletesBlock()
    {
        var pipeline = CreatePipeline();

        // Emit the call
        var callUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "GetWeather",
                new Dictionary<string, object?> { ["city"] = "Seattle" })],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks = await CollectBlocks(pipeline, callUpdate);
        var block = Assert.IsType<FunctionInvocationContentBlock>(blocks[0]);

        // Track NotifyChanged
        var changed = false;
        block.OnChanged(() => changed = true);

        // Process the result
        var resultUpdate = new ChatResponseUpdate
        {
            Contents = [new FunctionResultContent("call-1", "sunny, 72°F")]
        };
        await CollectBlocks(pipeline, resultUpdate);

        Assert.True(block.HasResult);
        Assert.Equal("sunny, 72°F", block.Result?.Result?.ToString());
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
        Assert.True(changed);
    }

    [Fact]
    public async Task FunctionResultContent_WrongCallId_DoesNotAffectBlock()
    {
        var pipeline = CreatePipeline();

        // Emit block for call-1
        var callUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "GetWeather", null)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        await CollectBlocks(pipeline, callUpdate);

        // Result for call-99 (doesn't match)
        var resultUpdate = new ChatResponseUpdate
        {
            Contents = [new FunctionResultContent("call-99", "wrong")]
        };
        var emitted = await CollectBlocks(pipeline, resultUpdate);

        // No new block emitted
        Assert.Empty(emitted);
    }

    [Fact]
    public async Task SequentialToolCalls_ProduceSeparateBlocks()
    {
        var pipeline = CreatePipeline();

        // First tool call
        var call1 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-A", "GetWeather", null)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks1 = await CollectBlocks(pipeline, call1);

        // Second tool call
        var call2 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-B", "GetNews", null)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks2 = await CollectBlocks(pipeline, call2);

        Assert.Single(blocks1);
        Assert.Single(blocks2);
        Assert.NotSame(blocks1[0], blocks2[0]);

        var blockA = Assert.IsType<FunctionInvocationContentBlock>(blocks1[0]);
        var blockB = Assert.IsType<FunctionInvocationContentBlock>(blocks2[0]);
        Assert.Equal("call-A", blockA.Id);
        Assert.Equal("call-B", blockB.Id);
    }

    [Fact]
    public async Task SequentialToolCalls_ResultsMatchCorrectBlocks()
    {
        var pipeline = CreatePipeline();

        // Two tool calls
        var call1 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-A", "GetWeather", null)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks1 = await CollectBlocks(pipeline, call1);
        var blockA = Assert.IsType<FunctionInvocationContentBlock>(blocks1[0]);

        var call2 = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-B", "GetNews", null)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks2 = await CollectBlocks(pipeline, call2);
        var blockB = Assert.IsType<FunctionInvocationContentBlock>(blocks2[0]);

        // Result for call-A
        var result1 = new ChatResponseUpdate
        {
            Contents = [new FunctionResultContent("call-A", "sunny")]
        };
        await CollectBlocks(pipeline, result1);

        Assert.True(blockA.HasResult);
        Assert.Equal("sunny", blockA.Result?.Result?.ToString());
        Assert.False(blockB.HasResult);

        // Result for call-B
        var result2 = new ChatResponseUpdate
        {
            Contents = [new FunctionResultContent("call-B", "headlines")]
        };
        await CollectBlocks(pipeline, result2);

        Assert.True(blockB.HasResult);
        Assert.Equal("headlines", blockB.Result?.Result?.ToString());
    }

    [Fact]
    public async Task Finalize_ActiveToolCallBlock_BecomesInactive()
    {
        var pipeline = CreatePipeline();

        var callUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "GetWeather", null)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks = await CollectBlocks(pipeline, callUpdate);
        var block = Assert.IsType<FunctionInvocationContentBlock>(blocks[0]);

        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);

        pipeline.Finalize();

        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
        Assert.False(block.HasResult);
    }
}

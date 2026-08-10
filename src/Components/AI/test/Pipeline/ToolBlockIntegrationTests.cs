// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class ToolBlockIntegrationTests
{
    private static BlockMappingPipeline CreatePipeline(Action<UIAgentOptions>? configure = null)
    {
        var options = new UIAgentOptions();
        options.AddGeneratedToolBlocks();
        configure?.Invoke(options);
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
    public async Task FunctionCallContent_MatchingToolName_EmitsTypedToolBlock()
    {
        var pipeline = CreatePipeline();

        var args = new Dictionary<string, object?>
        {
            ["Location"] = JsonSerializer.SerializeToElement("Seattle"),
            ["units"] = JsonSerializer.SerializeToElement("celsius")
        };

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "get_weather", args)],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<WeatherToolBlock>(blocks[0]);
        Assert.Equal("Seattle", block.Location);
        Assert.Equal("celsius", block.TemperatureUnits);
        Assert.Equal("call-1", block.Id);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
        Assert.False(block.HasResult);
    }

    [Fact]
    public async Task FunctionResultContent_MatchingCallId_CompletesTypedBlock()
    {
        var pipeline = CreatePipeline();

        var args = new Dictionary<string, object?>
        {
            ["Location"] = JsonSerializer.SerializeToElement("Seattle"),
            ["units"] = JsonSerializer.SerializeToElement("fahrenheit")
        };

        var callUpdate = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "get_weather", args)],
            FinishReason = ChatFinishReason.ToolCalls
        };
        var blocks = await CollectBlocks(pipeline, callUpdate);
        var block = Assert.IsType<WeatherToolBlock>(blocks[0]);

        // Process result
        var resultUpdate = new ChatResponseUpdate
        {
            Contents = [new FunctionResultContent("call-1", "sunny, 72°F")]
        };
        await CollectBlocks(pipeline, resultUpdate);

        Assert.True(block.HasResult);
        Assert.Equal("sunny, 72°F", block.Result?.Result?.ToString());
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }

    [Fact]
    public async Task IntParameter_DeserializedFromJsonElement()
    {
        var pipeline = CreatePipeline();

        var args = new Dictionary<string, object?>
        {
            ["q"] = JsonSerializer.SerializeToElement("dotnet"),
            ["MaxResults"] = JsonSerializer.SerializeToElement(5)
        };

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "search", args)],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<SearchToolBlock>(blocks[0]);
        Assert.Equal("dotnet", block.Query);
        Assert.Equal(5, block.MaxResults);
    }

    [Fact]
    public async Task UnmatchedToolCall_FallsThroughToGenericHandler()
    {
        var pipeline = CreatePipeline();

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "unknown_tool",
                new Dictionary<string, object?> { ["key"] = "val" })],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Single(blocks);
        // Should fall through to generic FunctionInvocationContentBlock, not a typed block
        var block = blocks[0];
        Assert.IsType<FunctionInvocationContentBlock>(block);
        Assert.IsNotType<WeatherToolBlock>(block);
        Assert.IsNotType<SearchToolBlock>(block);
    }

    [Fact]
    public async Task StringArgument_DirectString_NotJsonElement()
    {
        var pipeline = CreatePipeline();

        // Arguments as direct string values (not JsonElement)
        var args = new Dictionary<string, object?>
        {
            ["Location"] = "Portland",
            ["units"] = "metric"
        };

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "get_weather", args)],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Single(blocks);
        var block = Assert.IsType<WeatherToolBlock>(blocks[0]);
        Assert.Equal("Portland", block.Location);
        Assert.Equal("metric", block.TemperatureUnits);
    }

    [Fact]
    public async Task MissingArgument_PropertyRemainsDefault()
    {
        var pipeline = CreatePipeline();

        // Only provide Location, not units
        var args = new Dictionary<string, object?>
        {
            ["Location"] = JsonSerializer.SerializeToElement("Tokyo")
        };

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "get_weather", args)],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        var block = Assert.IsType<WeatherToolBlock>(blocks[0]);
        Assert.Equal("Tokyo", block.Location);
        Assert.Null(block.TemperatureUnits);
    }

    [Fact]
    public async Task NullArguments_HandlesGracefully()
    {
        var pipeline = CreatePipeline();

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent("call-1", "get_weather")],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        var block = Assert.IsType<WeatherToolBlock>(blocks[0]);
        Assert.Null(block.Location);
        Assert.Null(block.TemperatureUnits);
    }

    [Fact]
    public async Task MultipleToolCalls_EachGetsTypedBlock()
    {
        var pipeline = CreatePipeline();

        var weatherArgs = new Dictionary<string, object?>
        {
            ["Location"] = JsonSerializer.SerializeToElement("London")
        };
        var searchArgs = new Dictionary<string, object?>
        {
            ["q"] = JsonSerializer.SerializeToElement("restaurants"),
            ["MaxResults"] = JsonSerializer.SerializeToElement(10)
        };

        var update = new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents =
            [
                new FunctionCallContent("call-1", "get_weather", weatherArgs),
                new FunctionCallContent("call-2", "search", searchArgs)
            ],
            FinishReason = ChatFinishReason.ToolCalls
        };

        var blocks = await CollectBlocks(pipeline, update);

        Assert.Equal(2, blocks.Count);

        var weather = blocks.OfType<WeatherToolBlock>().Single();
        Assert.Equal("London", weather.Location);

        var search = blocks.OfType<SearchToolBlock>().Single();
        Assert.Equal("restaurants", search.Query);
        Assert.Equal(10, search.MaxResults);
    }
}

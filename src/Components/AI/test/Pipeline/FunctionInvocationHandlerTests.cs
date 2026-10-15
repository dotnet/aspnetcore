// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class FunctionInvocationHandlerTests
{
    [Fact]
    public async Task FunctionCall_EmitsActiveInvocationBlock()
    {
        var pipeline = CreatePipeline();
        var call = CreateCall("call-1", "get_weather");

        var blocks = await ProcessAsync(pipeline, CreateUpdate(call));

        var block = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(blocks));
        Assert.Same(call, block.Call);
        Assert.Equal("call-1", block.Id);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
        Assert.False(block.HasResult);
    }

    [Fact]
    public async Task InformationalOnlyCall_EmitsAndCompletesInvocationBlock()
    {
        var pipeline = CreatePipeline();
        var call = CreateCall("call-1", "get_weather");
        call.InformationalOnly = true;

        var blocks = await ProcessAsync(pipeline, CreateUpdate(call));
        var block = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(blocks));
        var result = new FunctionResultContent("call-1", "sunny");

        var emitted = await ProcessAsync(pipeline, CreateUpdate(result));

        Assert.Same(call, block.Call);
        Assert.Same(result, block.Result);
        Assert.Empty(emitted);
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }

    [Fact]
    public async Task MatchingResult_CompletesAndNotifiesInvocationBlock()
    {
        var pipeline = CreatePipeline();
        var blocks = await ProcessAsync(
            pipeline,
            CreateUpdate(CreateCall("call-1", "get_weather")));
        var block = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(blocks));
        var changeCount = 0;
        using var subscription = block.OnChanged(() => changeCount++);
        var result = new FunctionResultContent("call-1", "sunny");

        var emitted = await ProcessAsync(pipeline, CreateUpdate(result));

        Assert.Empty(emitted);
        Assert.Same(result, block.Result);
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
        Assert.Equal(1, changeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallAndResultInSameUpdate_CompletesInvocationBlock(bool resultFirst)
    {
        var pipeline = CreatePipeline();
        var call = CreateCall("call-1", "get_weather");
        var result = new FunctionResultContent("call-1", "sunny");
        var update = resultFirst
            ? CreateUpdate(result, call)
            : CreateUpdate(call, result);

        var blocks = await ProcessAsync(pipeline, update);

        var block = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(blocks));
        Assert.Same(call, block.Call);
        Assert.Same(result, block.Result);
        Assert.True(block.HasResult);
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }

    [Fact]
    public async Task MultipleCallsInOneUpdate_EmitSeparateActiveBlocks()
    {
        var pipeline = CreatePipeline();

        var blocks = await ProcessAsync(
            pipeline,
            CreateUpdate(
                CreateCall("call-weather", "get_weather"),
                CreateCall("call-news", "get_news")));

        Assert.Collection(
            blocks,
            block =>
            {
                var invocation = Assert.IsType<FunctionInvocationContentBlock>(block);
                Assert.Equal("call-weather", invocation.Id);
                Assert.Equal(BlockLifecycleState.Active, invocation.LifecycleState);
            },
            block =>
            {
                var invocation = Assert.IsType<FunctionInvocationContentBlock>(block);
                Assert.Equal("call-news", invocation.Id);
                Assert.Equal(BlockLifecycleState.Active, invocation.LifecycleState);
            });
    }

    [Fact]
    public async Task ResultsInReverseOrder_CompleteTheirMatchingBlocks()
    {
        var pipeline = CreatePipeline();
        var blocks = await ProcessAsync(
            pipeline,
            CreateUpdate(
                CreateCall("call-weather", "get_weather"),
                CreateCall("call-news", "get_news")));
        var weather = Assert.IsType<FunctionInvocationContentBlock>(blocks[0]);
        var news = Assert.IsType<FunctionInvocationContentBlock>(blocks[1]);
        var newsResult = new FunctionResultContent("call-news", "headlines");
        var weatherResult = new FunctionResultContent("call-weather", "sunny");

        await ProcessAsync(pipeline, CreateUpdate(newsResult, weatherResult));

        Assert.Same(weatherResult, weather.Result);
        Assert.Same(newsResult, news.Result);
        Assert.Equal(BlockLifecycleState.Inactive, weather.LifecycleState);
        Assert.Equal(BlockLifecycleState.Inactive, news.LifecycleState);
    }

    [Fact]
    public async Task ResultForOlderCall_DoesNotCompleteNewerActiveCall()
    {
        var pipeline = CreatePipeline();
        var weather = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(
            await ProcessAsync(pipeline, CreateUpdate(CreateCall("call-weather", "get_weather")))));
        var news = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(
            await ProcessAsync(pipeline, CreateUpdate(CreateCall("call-news", "get_news")))));
        var weatherResult = new FunctionResultContent("call-weather", "sunny");

        await ProcessAsync(pipeline, CreateUpdate(weatherResult));

        Assert.Same(weatherResult, weather.Result);
        Assert.True(weather.HasResult);
        Assert.False(news.HasResult);
        Assert.Equal(BlockLifecycleState.Inactive, weather.LifecycleState);
        Assert.Equal(BlockLifecycleState.Active, news.LifecycleState);
    }

    [Fact]
    public async Task MismatchedResult_DoesNotAffectActiveCalls()
    {
        var pipeline = CreatePipeline();
        var block = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(
            await ProcessAsync(pipeline, CreateUpdate(CreateCall("call-1", "get_weather")))));

        var emitted = await ProcessAsync(
            pipeline,
            CreateUpdate(new FunctionResultContent("call-other", "wrong")));

        Assert.Empty(emitted);
        Assert.False(block.HasResult);
        Assert.Equal(BlockLifecycleState.Active, block.LifecycleState);
    }

    [Fact]
    public async Task Finalize_DeactivatesInvocationWithoutManufacturingResult()
    {
        var pipeline = CreatePipeline();
        var block = Assert.IsType<FunctionInvocationContentBlock>(Assert.Single(
            await ProcessAsync(pipeline, CreateUpdate(CreateCall("call-1", "get_weather")))));

        pipeline.Finalize();

        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
        Assert.False(block.HasResult);
    }

    private static BlockMappingPipeline CreatePipeline() => new(new UIAgentOptions());

    private static FunctionCallContent CreateCall(string callId, string name) =>
        new(callId, name, new Dictionary<string, object?> { ["location"] = "Seattle" });

    private static ChatResponseUpdate CreateUpdate(params AIContent[] contents) => new()
    {
        Role = ChatRole.Assistant,
        Contents = contents,
    };

    private static async Task<List<ContentBlock>> ProcessAsync(
        BlockMappingPipeline pipeline,
        ChatResponseUpdate update)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in pipeline.Process(update))
        {
            blocks.Add(block);
        }

        return blocks;
    }
}

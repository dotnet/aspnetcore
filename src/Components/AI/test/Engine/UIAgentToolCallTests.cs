// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentToolCallTests
{
    [Fact]
    public async Task SendMessageAsync_ToolCallWithResult_ProducesSingleBlock()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitToolCallWithResultResponse(
                "call-1", "GetWeather",
                new Dictionary<string, object?> { ["city"] = "Seattle" },
                "sunny, 72°F"));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "What's the weather?")))
        {
            blocks.Add(block);
        }

        var toolBlocks = blocks.OfType<FunctionInvocationContentBlock>().ToList();
        Assert.Single(toolBlocks);
        Assert.Equal("GetWeather", toolBlocks[0].ToolName);
        Assert.True(toolBlocks[0].HasResult);
        Assert.Equal(BlockLifecycleState.Inactive, toolBlocks[0].LifecycleState);
    }

    [Fact]
    public async Task SendMessageAsync_ToolCallOnly_BlockBecomesInactiveAfterFinalize()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitToolCallResponse("call-1", "GetWeather"));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "weather?")))
        {
            blocks.Add(block);
        }

        var toolBlocks = blocks.OfType<FunctionInvocationContentBlock>().ToList();
        Assert.Single(toolBlocks);
        Assert.False(toolBlocks[0].HasResult);
        // Block should be Inactive after stream ends (Finalize transitions it)
        Assert.Equal(BlockLifecycleState.Inactive, toolBlocks[0].LifecycleState);
    }

    [Fact]
    public async Task SendMessageAsync_MixedTextAndToolCall_ProducesCorrectBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => YieldMixed(ct));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Search and explain")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        // Should have at least a FunctionInvocationContentBlock and a RichContentBlock
        Assert.Contains(assistantBlocks, b => b is FunctionInvocationContentBlock);
        Assert.Contains(assistantBlocks, b => b is RichContentBlock);

        static async IAsyncEnumerable<ChatResponseUpdate> YieldMixed(
            [EnumeratorCancellation] CancellationToken ct)
        {
            // Tool call
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new FunctionCallContent("call-1", "Search",
                    new Dictionary<string, object?> { ["q"] = "test" })],
                FinishReason = ChatFinishReason.ToolCalls
            };
            // Tool result
            yield return new ChatResponseUpdate
            {
                Contents = [new FunctionResultContent("call-1", "found 3 results")]
            };
            // Text
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "msg-2",
                Contents = [new TextContent("Here are the results...")]
            };
            await Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SendMessageAsync_SequentialToolCalls_ProducesSeparateBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => YieldSequentialCalls(ct));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Get weather and news")))
        {
            blocks.Add(block);
        }

        var toolBlocks = blocks.OfType<FunctionInvocationContentBlock>().ToList();
        Assert.Equal(2, toolBlocks.Count);
        Assert.Equal("call-A", toolBlocks[0].Id);
        Assert.Equal("call-B", toolBlocks[1].Id);
        Assert.True(toolBlocks[0].HasResult);
        Assert.True(toolBlocks[1].HasResult);

        static async IAsyncEnumerable<ChatResponseUpdate> YieldSequentialCalls(
            [EnumeratorCancellation] CancellationToken ct)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new FunctionCallContent("call-A", "GetWeather", null)],
                FinishReason = ChatFinishReason.ToolCalls
            };
            yield return new ChatResponseUpdate
            {
                Contents = [new FunctionResultContent("call-A", "sunny")]
            };
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new FunctionCallContent("call-B", "GetNews", null)],
                FinishReason = ChatFinishReason.ToolCalls
            };
            yield return new ChatResponseUpdate
            {
                Contents = [new FunctionResultContent("call-B", "headlines")]
            };
            await Task.CompletedTask;
        }
    }
}

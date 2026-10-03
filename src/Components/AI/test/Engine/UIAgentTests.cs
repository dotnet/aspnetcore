// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class UIAgentTests
{
    [Fact]
    public async Task SendMessageAsync_TextResponse_YieldsUserThenAssistantBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => ResponseEmitters.EmitTextResponse("Hi there!"));
        var agent = new UIAgent(client);

        var blocks = await CollectAsync(agent, "Hello");

        var userBlock = Assert.IsType<RichContentBlock>(
            Assert.Single(blocks.Where(b => b.Role == ChatRole.User)));
        Assert.Equal("Hello", userBlock.RawText);

        var assistantBlock = Assert.IsType<RichContentBlock>(
            Assert.Single(blocks.Where(b => b.Role == ChatRole.Assistant)));
        Assert.Equal("Hi there!", assistantBlock.RawText);
    }

    [Fact]
    public async Task SendMessageAsync_MultiTokenStreaming_SingleBlockWithAccumulatedText()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitMultiTokenTextResponse(ct, "Hello", " ", "world", "!"));
        var agent = new UIAgent(client);

        var blocks = await CollectAsync(agent, "Hi");

        var rich = Assert.IsType<RichContentBlock>(
            Assert.Single(blocks.Where(b => b.Role == ChatRole.Assistant)));
        Assert.Equal("Hello world!", rich.RawText);
        var paragraph = Assert.IsType<ParagraphNode>(Assert.Single(rich.Content));
        Assert.Equal("Hello world!", Assert.IsType<TextNode>(Assert.Single(paragraph.Children)).Text);
    }

    [Fact]
    public async Task SendMessageAsync_MultiTokenStreaming_OnChangedFiresPerToken()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitMultiTokenTextResponse(ct, "A", "B", "C"));
        var agent = new UIAgent(client);

        var changeCount = 0;
        ContentBlock? firstBlock = null;

        await foreach (var block in agent.SendMessageAsync(new ChatMessage(ChatRole.User, "Hi")))
        {
            if (block.Role == ChatRole.Assistant && firstBlock is null)
            {
                firstBlock = block;
                block.OnChanged(() => changeCount++);
            }
        }

        // Two updates after the block was emitted, plus the notification from finalizing it.
        Assert.Equal(3, changeCount);
    }

    [Fact]
    public async Task SendMessageAsync_AllBlocksInactiveAfterIteration()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => ResponseEmitters.EmitTextResponse("Done"));
        var agent = new UIAgent(client);

        var blocks = await CollectAsync(agent, "Hi");

        Assert.All(blocks, block => Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState));
    }

    [Fact]
    public async Task SendMessageAsync_SecondTurn_SendsPreviousMessagesAsHistory()
    {
        var client = new DelegatingStreamingChatClient();
        List<ChatMessage>? secondRequest = null;
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 2)
            {
                secondRequest = msgs.ToList();
            }

            return ResponseEmitters.EmitTextResponse($"Answer {callCount}");
        });
        var agent = new UIAgent(client);

        await CollectAsync(agent, "First question");
        var secondTurn = await CollectAsync(agent, "Second question");

        Assert.NotNull(secondRequest);
        Assert.Equal(3, secondRequest!.Count);
        Assert.Equal("First question", secondRequest[0].Text);
        Assert.Equal("Answer 1", secondRequest[1].Text);
        Assert.Equal("Second question", secondRequest[2].Text);

        var assistantBlock = Assert.IsType<RichContentBlock>(
            Assert.Single(secondTurn.Where(b => b.Role == ChatRole.Assistant)));
        Assert.Equal("Answer 2", assistantBlock.RawText);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyResponse_YieldsOnlyUserBlock()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => ResponseEmitters.EmitEmptyResponse(ct));
        var agent = new UIAgent(client);

        var blocks = await CollectAsync(agent, "Hi");

        Assert.Equal(ChatRole.User, Assert.Single(blocks).Role);
    }

    [Fact]
    public async Task SendMessageAsync_FailedAttemptsDoNotGrowHistory()
    {
        var requestHistoryCounts = new List<int>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, _, _) =>
        {
            requestHistoryCounts.Add(messages.Count());
            return ResponseEmitters.EmitErrorAfterTokens(
                ["partial"],
                new InvalidOperationException("boom"));
        });
        var agent = new UIAgent(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(agent, "Hello"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(agent, "Hello"));

        Assert.Equal([1, 1], requestHistoryCounts);
    }

    [Fact]
    public async Task SendMessageAsync_UsesConfiguredChatOptions()
    {
        var client = new DelegatingStreamingChatClient();
        ChatOptions? observed = null;
        client.SetHandler((msgs, opts, ct) =>
        {
            observed = opts;
            return ResponseEmitters.EmitTextResponse("ok");
        });
        var chatOptions = new ChatOptions { ModelId = "test-model" };
        var agent = new UIAgent(client, chatOptions);

        await CollectAsync(agent, "Hi");

        Assert.Same(chatOptions, observed);
    }

    [Fact]
    public void Constructor_NullChatClientThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new UIAgent(null!));
    }

    [Fact]
    public async Task SendMessageAsync_AfterDispose_Throws()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => ResponseEmitters.EmitTextResponse("ok"));
        var agent = new UIAgent(client);
        agent.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => CollectAsync(agent, "Hi"));
    }

    private static async Task<List<ContentBlock>> CollectAsync(UIAgent agent, string text)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(new ChatMessage(ChatRole.User, text)))
        {
            blocks.Add(block);
        }

        return blocks;
    }
}

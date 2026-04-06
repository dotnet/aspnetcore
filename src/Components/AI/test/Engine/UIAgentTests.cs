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
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi there!"));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hello")))
        {
            blocks.Add(block);
        }

        Assert.True(blocks.Count >= 2);

        var userBlocks = blocks.Where(b => b.Role == ChatRole.User).ToList();
        Assert.NotEmpty(userBlocks);
        var userBlock = Assert.IsType<RichContentBlock>(userBlocks[0]);
        Assert.Equal("Hello", userBlock.RawText);

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.NotEmpty(assistantBlocks);
        var assistantBlock = Assert.IsType<RichContentBlock>(assistantBlocks[0]);
        Assert.Equal("Hi there!", assistantBlock.RawText);
    }

    [Fact]
    public async Task SendMessageAsync_MultiTokenStreaming_SingleBlockWithAccumulatedText()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitMultiTokenTextResponse(ct, "Hello", " ", "world", "!"));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hi")))
        {
            blocks.Add(block);
        }

        var assistantBlocks = blocks.Where(b => b.Role == ChatRole.Assistant).ToList();
        Assert.Single(assistantBlocks);
        var rich = Assert.IsType<RichContentBlock>(assistantBlocks[0]);
        Assert.Equal("Hello world!", rich.RawText);
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

        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hi")))
        {
            if (block.Role == ChatRole.Assistant && firstBlock is null)
            {
                firstBlock = block;
                block.OnChanged(() => changeCount++);
            }
        }

        Assert.Equal(2, changeCount);
    }

    [Fact]
    public async Task SendMessageAsync_AllBlocksInactiveAfterIteration()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Done"));
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Go")))
        {
            blocks.Add(block);
        }

        Assert.All(blocks, b => Assert.Equal(BlockLifecycleState.Inactive, b.LifecycleState));
    }

    [Fact]
    public async Task SendMessageAsync_EmptyResponse_YieldsUserBlockOnly()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => ResponseEmitters.EmitEmptyResponse());
        var agent = new UIAgent(client);

        var blocks = new List<ContentBlock>();
        await foreach (var block in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "Hello")))
        {
            blocks.Add(block);
        }

        Assert.All(blocks, b => Assert.Equal(ChatRole.User, b.Role));
    }

    [Fact]
    public async Task SendMessageAsync_PassesChatOptions_ToIChatClient()
    {
        ChatOptions? capturedOptions = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            capturedOptions = opts;
            return ResponseEmitters.EmitTextResponse("ok");
        });
        var expectedOptions = new ChatOptions { Temperature = 0.5f };
        var agent = new UIAgent(client, expectedOptions);

        await foreach (var _ in agent.SendMessageAsync(
            new ChatMessage(ChatRole.User, "test"))) { }

        Assert.Same(expectedOptions, capturedOptions);
    }
}

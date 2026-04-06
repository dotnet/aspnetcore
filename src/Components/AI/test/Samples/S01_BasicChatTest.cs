// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S01_BasicChatTest
{
    [Fact]
    public async Task SingleMessage_ProducesTextBlock()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello! How can I help you?"));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Hi there");

        Assert.Single(context.Turns);
        var turn = context.Turns[0];

        // Request side: user text block
        Assert.Single(turn.RequestBlocks);
        var requestBlock = Assert.IsType<RichContentBlock>(turn.RequestBlocks[0]);
        Assert.Equal(ChatRole.User, requestBlock.Role);
        Assert.Equal("Hi there", requestBlock.RawText);

        // Response side: assistant text block
        Assert.Single(turn.ResponseBlocks);
        var responseBlock = Assert.IsType<RichContentBlock>(turn.ResponseBlocks[0]);
        Assert.Equal(ChatRole.Assistant, responseBlock.Role);
        Assert.Equal("Hello! How can I help you?", responseBlock.RawText);
    }

    [Fact]
    public async Task MultiTokenStreaming_AccumulatesInSingleBlock()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitMultiTokenTextResponse(ct, "The ", "weather ", "is ", "sunny."));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("What's the weather?");

        var turn = context.Turns[0];
        var responseBlock = Assert.IsType<RichContentBlock>(turn.ResponseBlocks.Single());
        Assert.Equal("The weather is sunny.", responseBlock.RawText);
        // 4 tokens form a single paragraph
        var paragraph = Assert.IsType<ParagraphNode>(Assert.Single(responseBlock.Content));
        var textNode = Assert.IsType<TextNode>(Assert.Single(paragraph.Children));
        Assert.Equal("The weather is sunny.", textNode.Text);
    }

    [Fact]
    public async Task StatusTransitions_IdleToStreamingToIdle()
    {
        var statuses = new List<ConversationStatus>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK"));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(s => statuses.Add(s));

        Assert.Equal(ConversationStatus.Idle, context.Status);

        await context.SendMessageAsync("Hello");

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Equal(ConversationStatus.Streaming, statuses[0]);
        Assert.Equal(ConversationStatus.Idle, statuses[^1]);
    }

    [Fact]
    public async Task MultiTurn_EachMessageCreatesNewTurn()
    {
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            return ResponseEmitters.EmitTextResponse(callCount == 1 ? "Hi!" : "I'm fine!");
        });

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Hello");
        await context.SendMessageAsync("How are you?");

        Assert.Equal(2, context.Turns.Count);
        Assert.Equal("Hi!", context.Turns[0].ResponseBlocks.OfType<RichContentBlock>().Single().RawText);
        Assert.Equal("I'm fine!", context.Turns[1].ResponseBlocks.OfType<RichContentBlock>().Single().RawText);
    }

    [Fact]
    public async Task BlockLifecycle_ActiveThenInactive()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("response"));

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        await context.SendMessageAsync("test");

        var block = context.Turns[0].ResponseBlocks.Single();
        Assert.Equal(BlockLifecycleState.Inactive, block.LifecycleState);
    }
}

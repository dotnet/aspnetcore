// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class ConversationTurnRendererTests
{
    [Fact]
    public void RenderTo_EmptyTurn_RendersEmptyTurnDiv()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        var turn = new ConversationTurn();
        var listContext = new MessageListContext();
        var renderCount = 0;

        var renderer = new ConversationTurnRenderer(
            context, turn, listContext, () => renderCount++);

        var testRenderer = new TestRenderer();
        var rendered = testRenderer.RenderComponent<RenderDelegateComponent>(p =>
        {
            p["RenderAction"] = (Action<RenderTreeBuilder>)(builder =>
            {
                renderer.RenderTo(builder, 0);
            });
        });

        var html = rendered.GetHtml();
        Assert.Contains("class=\"sc-ai-turn", html);

        renderer.Dispose();
        context.Dispose();
    }

    [Fact]
    public void RenderTo_TurnWithExistingBlocks_RendersAllBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        var turn = new ConversationTurn();

        var requestBlock = new RichContentBlock { Role = ChatRole.User };
        requestBlock.AppendText("Hello");
        turn.AddRequestBlock(requestBlock);

        var responseBlock = new RichContentBlock { Role = ChatRole.Assistant };
        responseBlock.AppendText("Hi there");
        turn.AddResponseBlock(responseBlock);

        var listContext = new MessageListContext();
        var renderCount = 0;

        var renderer = new ConversationTurnRenderer(
            context, turn, listContext, () => renderCount++);

        var testRenderer = new TestRenderer();
        var rendered = testRenderer.RenderComponent<RenderDelegateComponent>(p =>
        {
            p["RenderAction"] = (Action<RenderTreeBuilder>)(builder =>
            {
                renderer.RenderTo(builder, 0);
            });
        });

        var html = rendered.GetHtml();
        Assert.Contains("Hello", html);
        Assert.Contains("Hi there", html);

        renderer.Dispose();
        context.Dispose();
    }

    [Fact]
    public async Task OnBlockAdded_NewBlock_TriggersRequestRender()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Response", ct));
        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        var listContext = new MessageListContext();
        var renderCount = 0;
        ConversationTurnRenderer? renderer = null;

        // Listen for the turn the context creates, then create a renderer for it
        context.RegisterOnTurnAdded(turn =>
        {
            renderer = new ConversationTurnRenderer(
                context, turn, listContext, () => renderCount++);
        });

        await context.SendMessageAsync("Hello");

        // The renderer should have been notified about new blocks added during streaming
        Assert.NotNull(renderer);
        Assert.True(renderCount > 0);

        renderer!.Dispose();
        context.Dispose();
    }

    [Fact]
    public void BlockContainer_SubscribesToOnChanged()
    {
        var block = new RichContentBlock { Role = ChatRole.Assistant };
        block.AppendText("Initial");

        var listContext = new MessageListContext();
        var renderCount = 0;

        var container = new BlockContainer(block, listContext, () => renderCount++);

        // Modify the block — should trigger the render callback
        block.AppendText(" more");
        block.InvokeNotifyChanged();

        Assert.Equal(1, renderCount);

        // Modify again
        block.AppendText(" text");
        block.InvokeNotifyChanged();

        Assert.Equal(2, renderCount);

        container.Dispose();

        // After dispose, changes should NOT trigger callback
        block.AppendText(" after dispose");
        block.InvokeNotifyChanged();

        Assert.Equal(2, renderCount);
    }

    [Fact]
    public void Dispose_CleansUpSubscriptions()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);
        var context = new AgentContext(agent);
        var turn = new ConversationTurn();

        var block = new RichContentBlock { Role = ChatRole.Assistant };
        block.AppendText("Test");
        turn.AddResponseBlock(block);

        var listContext = new MessageListContext();
        var renderCount = 0;

        var renderer = new ConversationTurnRenderer(
            context, turn, listContext, () => renderCount++);

        renderer.Dispose();

        // After dispose, block changes should not trigger render
        block.AppendText(" more");
        block.InvokeNotifyChanged();

        Assert.Equal(0, renderCount);

        context.Dispose();
    }

    // Helper component that delegates BuildRenderTree to an Action
    private class RenderDelegateComponent : ComponentBase
    {
        [Parameter]
        public Action<RenderTreeBuilder>? RenderAction { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            RenderAction?.Invoke(builder);
        }
    }
}

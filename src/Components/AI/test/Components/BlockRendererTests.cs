// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class BlockRendererTests
{
    [Fact]
    public async Task InlineRenderer_OverridesDefaultRendering()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello!", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "ChildContent", (RenderFragment)(inner =>
                {
                    inner.OpenComponent<BlockRenderer<RichContentBlock>>(0);
                    inner.AddComponentParameter(1, "ChildContent",
                        new RenderFragment<RichContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", "custom-text");
                            builder.AddContent(2, block.RawText);
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("custom-text", html);
        Assert.Contains("Hello!", html);
        Assert.DoesNotContain("sc-ai-message__bubble", html);
    }

    [Fact]
    public async Task ComponentRenderer_RendersViaComponent()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello!", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "ChildContent", (RenderFragment)(inner =>
                {
                    inner.OpenComponent<ComponentBlockRenderer<RichContentBlock, CustomBubbleView>>(0);
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("custom-bubble", html);
        Assert.Contains("Hello!", html);
        Assert.DoesNotContain("sc-ai-message__bubble", html);
    }

    [Fact]
    public async Task WhenPredicate_RoutesToCorrectRenderer()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Reply", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "ChildContent", (RenderFragment)(inner =>
                {
                    // Renderer for user messages
                    inner.OpenComponent<BlockRenderer<RichContentBlock>>(0);
                    inner.AddComponentParameter(1, "When",
                        new Func<RichContentBlock, bool>(block =>
                            block.Role == ChatRole.User));
                    inner.AddComponentParameter(2, "ChildContent",
                        new RenderFragment<RichContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "div");
                            builder.AddAttribute(1, "class", "user-bubble");
                            builder.AddContent(2, block.RawText);
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();

                    // Renderer for assistant messages
                    inner.OpenComponent<BlockRenderer<RichContentBlock>>(10);
                    inner.AddComponentParameter(11, "When",
                        new Func<RichContentBlock, bool>(block =>
                            block.Role == ChatRole.Assistant));
                    inner.AddComponentParameter(12, "ChildContent",
                        new RenderFragment<RichContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "div");
                            builder.AddAttribute(1, "class", "assistant-bubble");
                            builder.AddContent(2, block.RawText);
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        // User message uses user-bubble renderer
        Assert.Contains("user-bubble", html);
        // Assistant message uses assistant-bubble renderer
        Assert.Contains("assistant-bubble", html);
        // Default renderer not used
        Assert.DoesNotContain("sc-ai-message__bubble", html);
    }

    [Fact]
    public async Task ResolutionOrder_FirstMatchWins()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "ChildContent", (RenderFragment)(inner =>
                {
                    // First renderer
                    inner.OpenComponent<BlockRenderer<RichContentBlock>>(0);
                    inner.AddComponentParameter(1, "ChildContent",
                        new RenderFragment<RichContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", "first-renderer");
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();

                    // Second renderer for same type
                    inner.OpenComponent<BlockRenderer<RichContentBlock>>(10);
                    inner.AddComponentParameter(11, "ChildContent",
                        new RenderFragment<RichContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", "second-renderer");
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("first-renderer", html);
        Assert.DoesNotContain("second-renderer", html);
    }

    [Fact]
    public async Task NoCustomRenderer_UsesBuiltInDefault()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello!", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-message", html);
        Assert.Contains("Hello!", html);
    }

    [Fact]
    public void GenericFallback_UnknownBlockType_ShowsTypeName()
    {
        var context = new MessageListContext();
        var block = new TestUnknownBlock();

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-tool-call", html);
        Assert.Contains("TestUnknownBlock", html);
    }

    [Fact]
    public async Task WhenPredicate_NoMatch_FallsToDefault()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello!", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "ChildContent", (RenderFragment)(inner =>
                {
                    // Renderer that only matches blocks containing "special"
                    inner.OpenComponent<BlockRenderer<RichContentBlock>>(0);
                    inner.AddComponentParameter(1, "When",
                        new Func<RichContentBlock, bool>(block =>
                            block.RawText.Contains("special")));
                    inner.AddComponentParameter(2, "ChildContent",
                        new RenderFragment<RichContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "div");
                            builder.AddAttribute(1, "class", "special-render");
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        // "Hello!" doesn't contain "special", so falls to default
        Assert.DoesNotContain("special-render", html);
        Assert.Contains("sc-ai-message", html);
    }

    [Fact]
    public async Task BaseTypeRenderer_MatchesDerivedBlocks()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hello!", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "ChildContent", (RenderFragment)(inner =>
                {
                    // Catch-all renderer for ContentBlock base type
                    inner.OpenComponent<BlockRenderer<ContentBlock>>(0);
                    inner.AddComponentParameter(1, "ChildContent",
                        new RenderFragment<ContentBlock>(block => builder =>
                        {
                            builder.OpenElement(0, "div");
                            builder.AddAttribute(1, "class", "catch-all");
                            builder.AddContent(2, block.GetType().Name);
                            builder.CloseElement();
                        }));
                    inner.CloseComponent();
                }));
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("catch-all", html);
        Assert.Contains("RichContentBlock", html);
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentBoundary> cut)
    {
        return (AgentContext)typeof(AgentBoundary)
            .GetField("_context",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    // Helper component to render a RenderFragment
    private class FragmentHost : ComponentBase
    {
        [Parameter]
        public RenderFragment? Fragment { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Fragment is not null)
            {
                builder.AddContent(0, Fragment);
            }
        }
    }

    // Test component that renders a RichContentBlock with custom markup
    internal class CustomBubbleView : ComponentBase
    {
        [Parameter]
        public RichContentBlock Block { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "custom-bubble");
            builder.AddContent(2, Block.RawText);
            builder.CloseElement();
        }
    }

    private class TestUnknownBlock : ContentBlock
    {
    }
}

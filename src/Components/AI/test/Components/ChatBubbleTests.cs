// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class ChatBubbleTests
{
    [Fact]
    public void RendersTriggerButton_Always()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatBubble>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-bubble__trigger", html);
        Assert.Contains("sc-ai-bubble--bottom-right", html);
    }

    [Fact]
    public void DoesNotRenderPanel_WhenClosed()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatBubble>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.DoesNotContain("sc-ai-bubble__panel", html);
    }

    [Fact]
    public void RendersBottomLeftPosition()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatBubble>(p =>
        {
            p["Agent"] = agent;
            p["Position"] = BubblePosition.BottomLeft;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-bubble--bottom-left", html);
    }

    [Fact]
    public void TriggerHasAccessibilityLabel()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatBubble>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.Contains("aria-label=\"Open chat\"", html);
    }

    [Fact]
    public void RendersRootClasses()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatBubble>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-root", html);
        Assert.Contains("sc-ai-bubble", html);
    }
}

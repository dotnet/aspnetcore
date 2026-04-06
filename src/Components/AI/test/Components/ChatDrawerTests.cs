// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class ChatDrawerTests
{
    [Fact]
    public void RendersNothing_WhenClosed()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatDrawer>(p =>
        {
            p["Agent"] = agent;
            p["Open"] = false;
        });

        var html = cut.GetHtml();
        Assert.Empty(html);
    }

    [Fact]
    public void RendersDrawerStructure_WhenOpen()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatDrawer>(p =>
        {
            p["Agent"] = agent;
            p["Open"] = true;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-root", html);
        Assert.Contains("sc-ai-drawer", html);
        Assert.Contains("sc-ai-drawer--right", html);
        Assert.Contains("sc-ai-drawer__header", html);
        Assert.Contains("sc-ai-drawer__body", html);
        Assert.Contains("sc-ai-drawer__footer", html);
    }

    [Fact]
    public void RendersTitle()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatDrawer>(p =>
        {
            p["Agent"] = agent;
            p["Open"] = true;
            p["Title"] = "Assistant";
        });

        var html = cut.GetHtml();
        Assert.Contains("Assistant", html);
    }

    [Fact]
    public void RendersLeftPosition()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatDrawer>(p =>
        {
            p["Agent"] = agent;
            p["Open"] = true;
            p["Position"] = DrawerPosition.Left;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-drawer--left", html);
    }

    [Fact]
    public void HasAccessibilityAttributes()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatDrawer>(p =>
        {
            p["Agent"] = agent;
            p["Open"] = true;
        });

        var html = cut.GetHtml();
        Assert.Contains("role=\"dialog\"", html);
        Assert.Contains("aria-label=\"Chat\"", html);
        Assert.Contains("aria-label=\"Close chat\"", html);
    }

    [Fact]
    public void RendersCloseButton()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatDrawer>(p =>
        {
            p["Agent"] = agent;
            p["Open"] = true;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-drawer__close", html);
    }
}

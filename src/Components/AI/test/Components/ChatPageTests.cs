// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class ChatPageTests
{
    [Fact]
    public void RendersPageStructure()
    {
        var cut = RenderChatPage(p => p["Agent"] = CreateAgent());

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-root", html);
        Assert.Contains("sc-ai-chat-page", html);
        Assert.Contains("sc-ai-chat-page__body", html);
        Assert.Contains("sc-ai-chat-page__footer", html);
        Assert.Contains("sc-ai-input", html);
        Assert.Contains("sc-ai-message-list", html);
    }

    [Fact]
    public void RendersHeaderAndWelcomeContent()
    {
        var cut = RenderChatPage(p =>
        {
            p["Agent"] = CreateAgent();
            p["Header"] = (RenderFragment)(b => b.AddMarkupContent(0, "<h1>Agentic Chat</h1>"));
            p["WelcomeContent"] = (RenderFragment)(b => b.AddMarkupContent(0, "<p>Say hello</p>"));
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-chat-page__header", html);
        Assert.Contains("Agentic Chat", html);
        Assert.Contains("Say hello", html);
    }

    [Fact]
    public void PlaceholderFlowsToMessageInput()
    {
        var cut = RenderChatPage(p =>
        {
            p["Agent"] = CreateAgent();
            p["Placeholder"] = "Ask me anything";
        });

        Assert.Contains("Ask me anything", cut.GetHtml());
    }

    [Fact]
    public void AdditionalAttributesAreAppliedToRootElement()
    {
        var cut = RenderChatPage(p =>
        {
            p["Agent"] = CreateAgent();
            p["data-scenario"] = "agentic_chat";
        });

        Assert.Contains("data-scenario=\"agentic_chat\"", cut.GetHtml());
    }

    private static UIAgent CreateAgent()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => ResponseEmitters.EmitTextResponse("OK", ct));
        return new UIAgent(client);
    }

    private static RenderedComponent<ChatPage> RenderChatPage(
        Action<Dictionary<string, object?>> configure)
    {
        var renderer = new TestRenderer();
        return renderer.RenderComponent<ChatPage>(configure);
    }
}

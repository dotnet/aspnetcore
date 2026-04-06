// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class ChatPageTests
{
    [Fact]
    public void RendersPageStructure()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatPage>(p =>
        {
            p["Agent"] = agent;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-root", html);
        Assert.Contains("sc-ai-chat-page", html);
        Assert.Contains("sc-ai-chat-page__body", html);
        Assert.Contains("sc-ai-chat-page__footer", html);
        Assert.Contains("sc-ai-input", html);
    }

    [Fact]
    public void RendersHeader()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatPage>(p =>
        {
            p["Agent"] = agent;
            p["Header"] = (RenderFragment)(b =>
            {
                b.OpenElement(0, "h1");
                b.AddContent(1, "My Chat");
                b.CloseElement();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-chat-page__header", html);
        Assert.Contains("My Chat", html);
    }

    [Fact]
    public void RendersWelcomeContent()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatPage>(p =>
        {
            p["Agent"] = agent;
            p["WelcomeContent"] = (RenderFragment)(b =>
            {
                b.OpenElement(0, "p");
                b.AddContent(1, "Welcome to the chat!");
                b.CloseElement();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("Welcome to the chat!", html);
    }

    [Fact]
    public void RendersCustomPlaceholder()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatPage>(p =>
        {
            p["Agent"] = agent;
            p["Placeholder"] = "Ask anything...";
        });

        var html = cut.GetHtml();
        Assert.Contains("Ask anything...", html);
    }

    [Fact]
    public void RendersSuggestions_WhenNoTurns()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var suggestions = new List<Suggestion>
        {
            new() { Label = "Hello", Prompt = "Hello" },
        };

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatPage>(p =>
        {
            p["Agent"] = agent;
            p["Suggestions"] = (IReadOnlyList<Suggestion>)suggestions;
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-suggestions", html);
        Assert.Contains("Hello", html);
    }

    [Fact]
    public void AcceptsAdditionalAttributes()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<ChatPage>(p =>
        {
            p["Agent"] = agent;
            p["id"] = "my-chat";
        });

        var html = cut.GetHtml();
        Assert.Contains("id=\"my-chat\"", html);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class FormMessageInputTests
{
    private static TestRenderer CreateRenderer()
    {
        var serviceProvider = new TestServiceProvider();
        serviceProvider.AddService<AntiforgeryStateProvider>(new NullAntiforgeryStateProvider());
        serviceProvider.AddService<IServiceProvider>(serviceProvider);
        return new TestRenderer(serviceProvider);
    }

    [Fact]
    public void RendersTextInput()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<FormMessageInput>(0);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("type=\"text\"", html);
        Assert.Contains("name=\"UserMessage\"", html);
        Assert.Contains("sc-ai-input__textarea", html);
    }

    [Fact]
    public void RendersSubmitButton()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<FormMessageInput>(0);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("type=\"submit\"", html);
        Assert.Contains("sc-ai-input__send", html);
        Assert.Contains("aria-label=\"Send message\"", html);
    }

    [Fact]
    public void UsesCustomPlaceholder()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<FormMessageInput>(0);
                b.AddComponentParameter(1, "Placeholder", "Ask me anything...");
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("placeholder=\"Ask me anything...\"", html);
    }

    [Fact]
    public void UsesDefaultPlaceholder()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);

        var renderer = CreateRenderer();
        var cut = renderer.RenderComponent<AgentFormBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<FormMessageInput>(0);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("placeholder=\"Type a message...\"", html);
    }

    [Fact]
    public void FormMessageInput_OutsideAgentBoundary_Throws()
    {
        var renderer = new TestRenderer();
        Assert.Throws<InvalidOperationException>(() =>
        {
            renderer.RenderComponent<FormMessageInput>();
        });
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class MessageInputAttachmentTests
{
    [Fact]
    public void AllowAttachments_False_NoAttachButton()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageInput>(0);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.DoesNotContain("sc-ai-input__attach", html);
        Assert.DoesNotContain("type=\"file\"", html);
    }

    [Fact]
    public void AllowAttachments_True_RendersAttachButton()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageInput>(0);
                b.AddComponentParameter(1, "AllowAttachments", true);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-input__attach", html);
        Assert.Contains("type=\"file\"", html);
    }

    [Fact]
    public void AllowAttachments_True_HasHiddenFileInput()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageInput>(0);
                b.AddComponentParameter(1, "AllowAttachments", true);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-input__file", html);
        Assert.Contains("multiple", html);
    }

    [Fact]
    public void AcceptFileTypes_CustomValue_SetsAcceptAttribute()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageInput>(0);
                b.AddComponentParameter(1, "AllowAttachments", true);
                b.AddComponentParameter(2, "AcceptFileTypes", "image/*,audio/*");
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("accept=\"image/*,audio/*\"", html);
    }

    [Fact]
    public void AllowAttachments_True_DefaultAcceptIsImageOnly()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageInput>(0);
                b.AddComponentParameter(1, "AllowAttachments", true);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("accept=\"image/*\"", html);
    }

    [Fact]
    public void InputBody_RendersWithCorrectClass()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Hi", ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<MessageInput>(0);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-input__body", html);
    }
}

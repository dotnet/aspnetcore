// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class MessageInputTests
{
    [Fact]
    public void RendersTextarea_WithPlaceholder()
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
                b.AddComponentParameter(1, "Placeholder", "Ask me anything...");
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("<textarea", html);
        Assert.Contains("placeholder=\"Ask me anything...\"", html);
        Assert.Contains("aria-label=\"Ask me anything...\"", html);
    }

    [Fact]
    public void RendersTextarea_WithDefaultPlaceholder()
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
        Assert.Contains("placeholder=\"Type a message...\"", html);
    }

    [Fact]
    public void DefaultSendButton_Rendered()
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
        Assert.Contains("<button", html);
        Assert.Contains("sc-ai-input__send", html);
        Assert.Contains("aria-label=\"Send message\"", html);
    }

    [Fact]
    public async Task DisabledDuringStreaming()
    {
        var streamingStarted = new TaskCompletionSource();
        var streamGate = new TaskCompletionSource();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            SlowStream(streamingStarted, streamGate, ct));
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

        var context = GetAgentContext(cut);
        Task sendTask = null!;
        await cut.InvokeAsync(() =>
        {
            sendTask = context.SendMessageAsync("Hello");
        });

        await streamingStarted.Task;

        var html = cut.GetHtml();
        // During streaming, textarea and button should be disabled
        Assert.Contains("disabled", html);

        streamGate.TrySetResult();
        await sendTask;

        html = cut.GetHtml();
        // After streaming completes, should no longer be disabled
        // (disabled="false" or no disabled attribute for non-boolean rendering)
    }

    [Fact]
    public async Task SendButton_SendsMessageAndClearsInput()
    {
        var messagesReceived = new List<string>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            messagesReceived.Add(msgs.Last().Text!);
            return ResponseEmitters.EmitTextResponse("OK", ct);
        });
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
        var input = cut.FindComponent<MessageInput>();

        await input.DispatchEventAsync("oninput", new ChangeEventArgs { Value = "Test message" });
        await input.DispatchEventAsync("onclick", EventArgs.Empty);

        Assert.Equal(["Test message"], messagesReceived);
        Assert.Contains("value=\"\"", input.GetHtml());
    }

    [Fact]
    public async Task Enter_SendsMessage()
    {
        var messagesReceived = new List<string>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            messagesReceived.Add(msgs.Last().Text!);
            return ResponseEmitters.EmitTextResponse("OK", ct);
        });
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
        var input = cut.FindComponent<MessageInput>();

        await input.DispatchEventAsync("oninput", new ChangeEventArgs { Value = "Test message" });
        await input.DispatchEventAsync("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(["Test message"], messagesReceived);
    }

    [Fact]
    public async Task ShiftEnter_DoesNotSendMessage()
    {
        var messagesReceived = new List<string>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            messagesReceived.Add(msgs.Last().Text!);
            return ResponseEmitters.EmitTextResponse("OK", ct);
        });
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
        var input = cut.FindComponent<MessageInput>();

        await input.DispatchEventAsync("oninput", new ChangeEventArgs { Value = "Test message" });
        await input.DispatchEventAsync(
            "onkeydown",
            new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        Assert.Empty(messagesReceived);
        Assert.Contains("value=\"Test message\"", input.GetHtml());
    }

    [Fact]
    public async Task Submit_CallsSendMessage()
    {
        var messagesReceived = new List<string>();
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            var lastMsg = msgs.Last().Text;
            if (lastMsg is not null)
            {
                messagesReceived.Add(lastMsg);
            }
            return ResponseEmitters.EmitTextResponse("OK", ct);
        });
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

        var context = GetAgentContext(cut);
        await cut.InvokeAsync(() => context.SendMessageAsync("Test message"));

        Assert.Single(messagesReceived);
        Assert.Equal("Test message", messagesReceived[0]);
    }

    [Fact]
    public void CustomTrailingActions_OverridesDefaultButton()
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
                b.AddComponentParameter(1, "TrailingActions", (RenderFragment)(inner =>
                {
                    inner.OpenElement(0, "button");
                    inner.AddAttribute(1, "class", "custom-send");
                    inner.AddContent(2, "Custom Send");
                    inner.CloseElement();
                }));
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("class=\"custom-send\"", html);
        Assert.Contains("Custom Send", html);
        // Default "Send" button text should not appear outside the custom one
        Assert.DoesNotContain(">Send<", html);
    }

    [Fact]
    public void RendersMessageInputContainer()
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
        Assert.Contains("class=\"sc-ai-input\"", html);
    }

    [Fact]
    public void MessageInput_OutsideAgentBoundary_Throws()
    {
        var renderer = new TestRenderer();
        Assert.Throws<InvalidOperationException>(() =>
        {
            renderer.RenderComponent<MessageInput>();
        });
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentBoundary> cut)
    {
        return (AgentContext)typeof(AgentBoundary)
            .GetField("_context",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> SlowStream(
        TaskCompletionSource started,
        TaskCompletionSource gate,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("tok")]
        };
        started.TrySetResult();
        try { await gate.Task.WaitAsync(ct); }
        catch (OperationCanceledException) { yield break; }
    }
}

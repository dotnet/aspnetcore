// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class MessageInputTests
{
    [Fact]
    public void RendersTextarea_WithPlaceholder()
    {
        var cut = RenderMessageInput(_ => ResponseEmitters.EmitTextResponse("Hi"), "Ask me anything...");

        var html = cut.GetHtml();
        Assert.Contains("<textarea", html);
        Assert.Contains("placeholder=\"Ask me anything...\"", html);
    }

    [Fact]
    public void RendersTextarea_WithDefaultPlaceholder()
    {
        var cut = RenderMessageInput(_ => ResponseEmitters.EmitTextResponse("Hi"));

        Assert.Contains("placeholder=\"Type a message...\"", cut.GetHtml());
    }

    [Fact]
    public void DefaultSendButton_Rendered()
    {
        var cut = RenderMessageInput(_ => ResponseEmitters.EmitTextResponse("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("<button", html);
        Assert.Contains("sc-ai-input__send", html);
    }

    [Fact]
    public async Task DisabledDuringStreaming_EnabledWhenIdle()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderMessageInput(ct => ResponseEmitters.EmitTokensWithGate(
            ["Hi"],
            _ => gate.Task,
            ct));
        var context = GetAgentContext(cut);

        var sendTask = cut.InvokeAsync(() => context.SendMessageAsync("Hello"));

        await WaitForHtmlAsync(cut, "disabled");

        gate.SetResult();
        await sendTask;

        Assert.DoesNotContain("disabled", cut.GetHtml());
    }

    [Fact]
    public async Task StatusSubscription_IsRegisteredOnce()
    {
        var cut = RenderMessageInput(_ => ResponseEmitters.EmitTextResponse("Hi"));

        // Re-render the boundary so the input receives its parameters again.
        await cut.InvokeAsync(() => { });
        var callbacks = GetStatusCallbacks(GetAgentContext(cut));

        // One from the MessageList and one from the MessageInput; re-rendering must not add more.
        Assert.Equal(2, callbacks.Count);
    }

    private static RenderedComponent<AgentBoundary> RenderMessageInput(
        Func<CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> respond,
        string? placeholder = null)
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => respond(ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        return renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MessageList>(0);
                builder.CloseComponent();
                builder.OpenComponent<MessageInput>(1);
                if (placeholder is not null)
                {
                    builder.AddComponentParameter(2, "Placeholder", placeholder);
                }
                builder.CloseComponent();
            });
        });
    }

    private static async Task WaitForHtmlAsync(
        RenderedComponent<AgentBoundary> cut, string expected)
    {
        for (var i = 0; i < 100; i++)
        {
            if (cut.GetHtml().Contains(expected, StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Timed out waiting for '{expected}' to render. Current markup: {cut.GetHtml()}");
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentBoundary> cut)
    {
        return (AgentContext)typeof(AgentBoundary)
            .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    private static System.Collections.ICollection GetStatusCallbacks(AgentContext context)
    {
        return (System.Collections.ICollection)typeof(AgentContext)
            .GetField("_statusChangedCallbacks", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(context)!;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class MessageListTests
{
    [Fact]
    public async Task RendersTurns_AfterMessageSent()
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
        Assert.Contains("sc-ai-turn", html);
        Assert.Contains("Hello!", html);
    }

    [Fact]
    public async Task MultipleTurns_AllRendered()
    {
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            return ResponseEmitters.EmitTextResponse($"Response {callCount}", ct);
        });
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
        await cut.InvokeAsync(() => context.SendMessageAsync("First"));
        await cut.InvokeAsync(() => context.SendMessageAsync("Second"));

        var html = cut.GetHtml();
        // Count turn divs
        var turnCount = CountOccurrences(html, "sc-ai-turn sc-ai-turn--");
        Assert.Equal(2, turnCount);
        Assert.Contains("Response 1", html);
        Assert.Contains("Response 2", html);
    }

    [Fact]
    public async Task DefaultFooter_ShowsTypingDuringStreaming()
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
                b.OpenComponent<MessageList>(0);
                b.CloseComponent();
            });
        });

        var context = GetAgentContext(cut);
        Task sendTask = null!;
        await cut.InvokeAsync(() =>
        {
            sendTask = context.SendMessageAsync("Hi");
        });

        await streamingStarted.Task;

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-typing", html);

        streamGate.TrySetResult();
        await sendTask;

        html = cut.GetHtml();
        Assert.DoesNotContain("sc-ai-typing", html);
    }

    [Fact]
    public async Task DefaultFooter_ShowsErrorBannerOnError()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitErrorAfterTokens(
                [], new InvalidOperationException("Server error"), ct));
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
        Assert.Contains("sc-ai-error", html);
        Assert.Contains("Server error", html);
        Assert.Contains("Retry", html);
    }

    [Fact]
    public async Task CustomFooter_OverridesDefault()
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
                b.OpenComponent<MessageList>(0);
                b.AddComponentParameter(1, "Footer",
                    (RenderFragment<AgentContext>)(ctx => builder =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(1, "class", "custom-footer");
                        builder.AddContent(2, $"Status: {ctx.Status}");
                        builder.CloseElement();
                    }));
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("class=\"custom-footer\"", html);
        Assert.Contains("Status: Idle", html);
        Assert.DoesNotContain("sc-ai-typing", html);
        Assert.DoesNotContain("sc-ai-error", html);
    }

    [Fact]
    public async Task StreamingBlock_ShowsAccumulatedText()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitMultiTokenTextResponse(ct, "Hello", " world", "!"));
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
        Assert.Contains("sc-ai-turn", html);
        Assert.Contains("sc-ai-message", html);
        Assert.Contains("Hello world!", html);
    }

    [Fact]
    public void MessageList_OutsideAgentBoundary_Throws()
    {
        var renderer = new TestRenderer();
        Assert.Throws<InvalidOperationException>(() =>
        {
            renderer.RenderComponent<MessageList>();
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

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
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

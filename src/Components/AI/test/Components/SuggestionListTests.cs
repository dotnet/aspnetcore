// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class SuggestionListTests
{
    [Fact]
    public void RendersSuggestionChips()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var suggestions = new List<Suggestion>
        {
            new() { Label = "Tell me a joke", Prompt = "Tell me a joke" },
            new() { Label = "What is the weather", Prompt = "What is the weather today?" },
        };

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<SuggestionList>(0);
                b.AddComponentParameter(1, "Suggestions", (IReadOnlyList<Suggestion>)suggestions);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-suggestions", html);
        Assert.Contains("Tell me a joke", html);
        Assert.Contains("What is the weather", html);
    }

    [Fact]
    public void HasAccessibilityAttributes()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("OK", ct));
        var agent = new UIAgent(client);

        var suggestions = new List<Suggestion>
        {
            new() { Label = "Help", Prompt = "Help me" },
        };

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<SuggestionList>(0);
                b.AddComponentParameter(1, "Suggestions", (IReadOnlyList<Suggestion>)suggestions);
                b.CloseComponent();
            });
        });

        var html = cut.GetHtml();
        Assert.Contains("role=\"group\"", html);
        Assert.Contains("aria-label=\"Suggestions\"", html);
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
        var suggestions = new List<Suggestion>
        {
            new() { Label = "Help", Prompt = "Help me" },
        };

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(b =>
            {
                b.OpenComponent<SuggestionList>(0);
                b.AddComponentParameter(1, "Suggestions", (IReadOnlyList<Suggestion>)suggestions);
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

        Assert.Contains("disabled", cut.GetHtml());

        streamGate.TrySetResult();
        await sendTask;
    }

    [Fact]
    public void SuggestionList_OutsideAgentBoundary_Throws()
    {
        var renderer = new TestRenderer();
        Assert.Throws<InvalidOperationException>(() =>
        {
            var suggestions = new List<Suggestion>
            {
                new() { Label = "Test", Prompt = "Test" },
            };
            renderer.RenderComponent<SuggestionList>(p =>
            {
                p["Suggestions"] = (IReadOnlyList<Suggestion>)suggestions;
            });
        });
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentBoundary> cut)
    {
        return (AgentContext)typeof(AgentBoundary)
            .GetField(
                "_context",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> SlowStream(
        TaskCompletionSource started,
        TaskCompletionSource gate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("tok")]
        };
        started.TrySetResult();
        try
        {
            await gate.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            yield break;
        }
    }
}

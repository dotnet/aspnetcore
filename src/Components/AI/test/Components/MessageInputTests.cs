// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
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
    public void TrailingActions_ReplaceDefaultSendButton()
    {
        var cut = RenderMessageInput(
            _ => ResponseEmitters.EmitTextResponse("Hi"),
            trailingActions: builder => builder.AddContent(0, "Custom action"));

        var html = cut.GetHtml();
        Assert.Contains("Custom action", html);
        Assert.DoesNotContain("sc-ai-input__send", html);
    }

    [Fact]
    public async Task DataContentAttachment_IsIncludedInSubmittedMessage()
    {
        MessageInputContext? inputContext = null;
        IReadOnlyList<ChatMessage>? submittedMessages = null;
        var cut = RenderMessageInput(
            _ => ResponseEmitters.EmitTextResponse("Hi"),
            topContent: context => builder =>
            {
                inputContext = context;
                builder.AddContent(0, "Composer");
            },
            onRequest: messages => submittedMessages = messages.ToArray());
        var attachment = new DataContent(new byte[] { 1, 2, 3 }, "image/png")
        {
            Name = "damage.png",
        };

        await cut.InvokeAsync(() => inputContext!.AddAttachmentAsync(attachment).AsTask());
        await cut.InvokeAsync(() => inputContext!.SubmitAsync());

        var userMessage = Assert.Single(submittedMessages!.Where(
            message => message.Role == ChatRole.User));
        Assert.Same(attachment, Assert.Single(userMessage.Contents.OfType<DataContent>()));
    }

    [Fact]
    public void Textarea_InputEventUpdatesValueAttribute()
    {
        var cut = RenderMessageInput(_ => ResponseEmitters.EmitTextResponse("Hi"));
        var cascadingValue = cut.FindComponent<CascadingValue<MessageInputContext>>();
        var frames = cascadingValue.GetFrames();
        var inputHandler = frames.Array
            .Take(frames.Count)
            .Single(frame =>
                frame.FrameType == RenderTreeFrameType.Attribute &&
                frame.AttributeName == "oninput");

        Assert.Equal("value", inputHandler.AttributeEventUpdatesAttributeName);
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

        await WaitForTextareaDisabledAsync(cut);

        gate.SetResult();
        await sendTask;

        Assert.DoesNotContain("disabled", GetTextareaHtml(cut.GetHtml()));
    }

    [Fact]
    public async Task Enter_SubmitsThroughBlazorHandler()
    {
        MessageInputContext? inputContext = null;
        var requestCount = 0;
        var cut = RenderMessageInput(
            _ => ResponseEmitters.EmitTextResponse("Hi"),
            topContent: context => builder => inputContext = context,
            onRequest: _ => requestCount++);
        await cut.InvokeAsync(() =>
        {
            inputContext!.Text = "Hello";
            return GetKeyDownCallback(cut)(new KeyboardEventArgs
            {
                Key = "Enter",
            });
        });

        Assert.Equal(1, requestCount);
        Assert.Equal(string.Empty, inputContext!.Text);
    }

    [Theory]
    [InlineData("Enter", true, false, false)]
    [InlineData("Enter", false, true, false)]
    [InlineData("Enter", false, false, true)]
    [InlineData("a", false, false, false)]
    public async Task KeyDown_DoesNotSubmitForNonSubmissionCombinations(
        string key,
        bool shiftKey,
        bool isComposing,
        bool repeat)
    {
        MessageInputContext? inputContext = null;
        var requestCount = 0;
        var cut = RenderMessageInput(
            _ => ResponseEmitters.EmitTextResponse("Hi"),
            topContent: context => builder => inputContext = context,
            onRequest: _ => requestCount++);
        await cut.InvokeAsync(() =>
        {
            inputContext!.Text = "Hello";
            return GetKeyDownCallback(cut)(new KeyboardEventArgs
            {
                Key = key,
                ShiftKey = shiftKey,
                IsComposing = isComposing,
                Repeat = repeat,
            });
        });

        Assert.Equal(0, requestCount);
        Assert.Equal("Hello", inputContext!.Text);
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
        string? placeholder = null,
        RenderFragment? trailingActions = null,
        RenderFragment<MessageInputContext>? topContent = null,
        Action<IReadOnlyList<ChatMessage>>? onRequest = null)
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            onRequest?.Invoke(messages.ToArray());
            return respond(cancellationToken);
        });
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
                if (trailingActions is not null)
                {
                    builder.AddComponentParameter(3, "TrailingActions", trailingActions);
                }
                if (topContent is not null)
                {
                    builder.AddComponentParameter(4, "TopContent", topContent);
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

    private static async Task WaitForTextareaDisabledAsync(
        RenderedComponent<AgentBoundary> cut)
    {
        for (var i = 0; i < 100; i++)
        {
            if (GetTextareaHtml(cut.GetHtml()).Contains("disabled", StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Timed out waiting for the text area to be disabled. Current markup: {cut.GetHtml()}");
    }

    private static string GetTextareaHtml(string html)
    {
        var start = html.IndexOf("<textarea", StringComparison.Ordinal);
        var end = html.IndexOf("</textarea>", start, StringComparison.Ordinal);
        return html[start..(end + "</textarea>".Length)];
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentBoundary> cut)
    {
        return (AgentContext)typeof(AgentBoundary)
            .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    private static Func<KeyboardEventArgs, Task> GetKeyDownCallback(
        RenderedComponent<AgentBoundary> cut)
    {
        var cascadingValue = cut.FindComponent<CascadingValue<MessageInputContext>>();
        var frames = cascadingValue.GetFrames();
        var callback = frames.Array
            .Take(frames.Count)
            .Single(frame =>
                frame.FrameType == RenderTreeFrameType.Attribute &&
                frame.AttributeName == "onkeydown")
            .AttributeValue;
        return callback switch
        {
            Func<KeyboardEventArgs, Task> handler => handler,
            EventCallback<KeyboardEventArgs> eventCallback =>
                eventArgs => eventCallback.InvokeAsync(eventArgs),
            _ => throw new InvalidOperationException(
                $"Unexpected keydown callback type {callback?.GetType().FullName}."),
        };
    }

    private static System.Collections.ICollection GetStatusCallbacks(AgentContext context)
    {
        return (System.Collections.ICollection)typeof(AgentContext)
            .GetField("_statusChangedCallbacks", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(context)!;
    }
}

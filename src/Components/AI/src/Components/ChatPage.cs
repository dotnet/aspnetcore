// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

public sealed class ChatPage : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter]
    public RenderFragment? WelcomeContent { get; set; }

    [Parameter]
    public IReadOnlyList<Suggestion>? Suggestions { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public RenderFragment? InputLeadingActions { get; set; }

    [Parameter]
    public RenderFragment? InputTrailingActions { get; set; }

    [Parameter]
    public bool AllowAttachments { get; set; }

    [Parameter]
    public string? AcceptFileTypes { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass());

        // Header
        if (Header is not null)
        {
            builder.OpenElement(10, "div");
            builder.AddAttribute(11, "class", "sc-ai-chat-page__header");
            builder.AddContent(12, Header);
            builder.CloseElement();
        }

        // AgentBoundary
        builder.OpenComponent<AgentBoundary>(20);
        builder.AddComponentParameter(21, "Agent", Agent);
        builder.AddComponentParameter(22, "ChildContent", (RenderFragment)(inner =>
        {
            // Body (scrollable area)
            inner.OpenElement(30, "div");
            inner.AddAttribute(31, "class", "sc-ai-chat-page__body");

            inner.OpenComponent<MessageList>(32);
            inner.AddComponentParameter(33, "ChildContent", WelcomeContent);
            inner.AddComponentParameter(34, "Footer", (RenderFragment<AgentContext>)(ctx =>
                (RenderFragment)(footerBuilder =>
            {
                RenderDefaultFooter(footerBuilder, ctx, 0);
            })));
            inner.CloseComponent(); // MessageList

            inner.CloseElement(); // body

            // Footer (input area)
            inner.OpenElement(50, "div");
            inner.AddAttribute(51, "class", "sc-ai-chat-page__footer");
            inner.OpenElement(52, "div");
            inner.AddAttribute(53, "class", "sc-ai-chat-page__input-container");

            inner.OpenComponent<MessageInput>(54);
            inner.AddComponentParameter(55, "Placeholder", Placeholder);
            inner.AddComponentParameter(56, "LeadingActions", InputLeadingActions);
            inner.AddComponentParameter(57, "TrailingActions", InputTrailingActions);
            inner.AddComponentParameter(58, "AllowAttachments", AllowAttachments);
            inner.AddComponentParameter(59, "AcceptFileTypes", AcceptFileTypes);
            inner.CloseComponent(); // MessageInput

            inner.CloseElement(); // input-container
            inner.CloseElement(); // footer
        }));
        builder.CloseComponent(); // AgentBoundary

        builder.CloseElement(); // root div
    }

    private void RenderDefaultFooter(RenderTreeBuilder builder, AgentContext ctx, int seq)
    {
        builder.OpenElement(seq, "div");
        builder.AddAttribute(seq + 1, "class", "sc-ai-message-list__footer");

        switch (ctx.Status)
        {
            case ConversationStatus.Streaming:
                builder.OpenElement(seq + 2, "div");
                builder.AddAttribute(seq + 3, "class", "sc-ai-typing");
                builder.AddAttribute(seq + 4, "role", "status");
                builder.AddAttribute(seq + 5, "aria-label", "Agent is typing");
                for (var i = 0; i < 3; i++)
                {
                    builder.OpenElement(seq + 6 + i, "span");
                    builder.AddAttribute(seq + 9 + i, "class", "sc-ai-typing__dot");
                    builder.CloseElement();
                }
                builder.CloseElement();
                break;

            case ConversationStatus.Error:
                builder.OpenElement(seq + 2, "div");
                builder.AddAttribute(seq + 3, "class", "sc-ai-error");
                builder.AddAttribute(seq + 4, "role", "alert");
                builder.OpenElement(seq + 5, "span");
                builder.AddAttribute(seq + 6, "class", "sc-ai-error__message");
                builder.AddContent(seq + 7, ctx.Error?.Message ?? "Something went wrong.");
                builder.CloseElement();
                builder.OpenElement(seq + 8, "button");
                builder.AddAttribute(seq + 9, "class", "sc-ai-btn sc-ai-btn--secondary");
                builder.AddAttribute(seq + 10, "onclick",
                    EventCallback.Factory.Create(this, () => ctx.RetryAsync()));
                builder.AddContent(seq + 11, "Retry");
                builder.CloseElement();
                builder.CloseElement();
                break;
        }

        if (Suggestions is { Count: > 0 } && ctx.Turns.Count == 0)
        {
            builder.OpenComponent<SuggestionList>(seq + 20);
            builder.AddComponentParameter(seq + 21, "Suggestions", Suggestions);
            builder.CloseComponent();
        }

        builder.CloseElement(); // footer div
    }

    private string CssClass()
    {
        var css = "sc-ai-root sc-ai-chat-page";
        if (AdditionalAttributes?.TryGetValue("class", out var existing) == true && existing is string s)
        {
            css = $"{css} {s}";
        }
        return css;
    }

    public void Dispose()
    {
        // Layout shell does not own the Agent — caller does.
    }
}

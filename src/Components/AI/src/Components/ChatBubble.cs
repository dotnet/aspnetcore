// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

public sealed class ChatBubble : ComponentBase
{
    private bool _isOpen;

    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    [Parameter]
    public BubblePosition Position { get; set; } = BubblePosition.BottomRight;

    [Parameter]
    public string Title { get; set; } = "Chat";

    [Parameter]
    public IReadOnlyList<Suggestion>? Suggestions { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

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

        if (_isOpen)
        {
            RenderPanel(builder, 10);
        }

        RenderTrigger(builder, 200);

        builder.CloseElement(); // root div
    }

    private void RenderPanel(RenderTreeBuilder builder, int seq)
    {
        builder.OpenElement(seq, "div");
        builder.AddAttribute(seq + 1, "class", "sc-ai-bubble__panel");
        builder.AddAttribute(seq + 2, "role", "dialog");
        builder.AddAttribute(seq + 3, "aria-label", Title);

        // Header
        builder.OpenElement(seq + 10, "div");
        builder.AddAttribute(seq + 11, "class", "sc-ai-bubble__header");

        builder.OpenElement(seq + 12, "span");
        builder.AddAttribute(seq + 13, "class", "sc-ai-bubble__title");
        builder.AddContent(seq + 14, Title);
        builder.CloseElement();

        builder.OpenElement(seq + 15, "button");
        builder.AddAttribute(seq + 16, "class", "sc-ai-bubble__close");
        builder.AddAttribute(seq + 17, "aria-label", "Close chat");
        builder.AddAttribute(seq + 18, "onclick", EventCallback.Factory.Create(this, Close));
        builder.AddMarkupContent(seq + 19, "&#x2715;");
        builder.CloseElement();

        builder.CloseElement(); // header

        // AgentBoundary
        builder.OpenComponent<AgentBoundary>(seq + 30);
        builder.AddComponentParameter(seq + 31, "Agent", Agent);
        builder.AddComponentParameter(seq + 32, "ChildContent", (RenderFragment)(inner =>
        {
            // Body
            inner.OpenElement(seq + 40, "div");
            inner.AddAttribute(seq + 41, "class", "sc-ai-bubble__body");

            inner.OpenComponent<MessageList>(seq + 42);
            inner.AddComponentParameter(seq + 43, "Footer", (RenderFragment<AgentContext>)(ctx =>
                (RenderFragment)(footerBuilder =>
            {
                RenderDefaultFooter(footerBuilder, ctx, 0);
            })));
            inner.CloseComponent();

            inner.CloseElement(); // body

            // Footer
            inner.OpenElement(seq + 50, "div");
            inner.AddAttribute(seq + 51, "class", "sc-ai-bubble__footer");

            inner.OpenComponent<MessageInput>(seq + 52);
            inner.AddComponentParameter(seq + 53, "Placeholder", Placeholder);
            inner.AddComponentParameter(seq + 54, "AllowAttachments", AllowAttachments);
            inner.AddComponentParameter(seq + 55, "AcceptFileTypes", AcceptFileTypes);
            inner.CloseComponent();

            inner.CloseElement(); // footer
        }));
        builder.CloseComponent(); // AgentBoundary

        builder.CloseElement(); // panel
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

    private void RenderTrigger(RenderTreeBuilder builder, int seq)
    {
        builder.OpenElement(seq, "button");
        builder.AddAttribute(seq + 1, "class", "sc-ai-bubble__trigger");
        builder.AddAttribute(seq + 2, "aria-label", _isOpen ? "Close chat" : "Open chat");
        builder.AddAttribute(seq + 3, "onclick", EventCallback.Factory.Create(this, Toggle));

        if (_isOpen)
        {
            // Close X icon
            builder.AddMarkupContent(seq + 4,
                "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
                "<path d=\"M18 6 6 18\"/><path d=\"M6 6 18 18\"/></svg>");
        }
        else
        {
            // Chat icon
            builder.AddMarkupContent(seq + 4,
                "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
                "<path d=\"M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z\"/></svg>");
        }

        builder.CloseElement(); // button
    }

    private void Toggle()
    {
        _isOpen = !_isOpen;
    }

    private void Close()
    {
        _isOpen = false;
    }

    private string CssClass()
    {
        var position = Position == BubblePosition.BottomLeft ? "bottom-left" : "bottom-right";
        var css = $"sc-ai-root sc-ai-bubble sc-ai-bubble--{position}";
        if (AdditionalAttributes?.TryGetValue("class", out var existing) == true && existing is string s)
        {
            css = $"{css} {s}";
        }
        return css;
    }
}

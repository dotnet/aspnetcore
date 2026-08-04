// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

public sealed class ChatDrawer : ComponentBase
{
    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public string Title { get; set; } = "Chat";

    [Parameter]
    public RenderFragment? HeaderActions { get; set; }

    [Parameter]
    public IReadOnlyList<Suggestion>? Suggestions { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public DrawerPosition Position { get; set; } = DrawerPosition.Right;

    [Parameter]
    public bool AllowAttachments { get; set; }

    [Parameter]
    public string? AcceptFileTypes { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Open)
        {
            return;
        }

        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass());
        builder.AddAttribute(3, "role", "dialog");
        builder.AddAttribute(4, "aria-label", Title);

        // Header
        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "class", "sc-ai-drawer__header");

        builder.OpenElement(12, "span");
        builder.AddAttribute(13, "class", "sc-ai-drawer__title");
        builder.AddContent(14, Title);
        builder.CloseElement(); // title

        if (HeaderActions is not null)
        {
            builder.AddContent(15, HeaderActions);
        }

        builder.OpenElement(16, "button");
        builder.AddAttribute(17, "class", "sc-ai-drawer__close");
        builder.AddAttribute(18, "aria-label", "Close chat");
        builder.AddAttribute(19, "onclick", EventCallback.Factory.Create(this, CloseAsync));
        builder.AddMarkupContent(20, "&#x2715;");
        builder.CloseElement(); // close button

        builder.CloseElement(); // header

        // AgentBoundary
        builder.OpenComponent<AgentBoundary>(30);
        builder.AddComponentParameter(31, "Agent", Agent);
        builder.AddComponentParameter(32, "ChildContent", (RenderFragment)(inner =>
        {
            // Body
            inner.OpenElement(40, "div");
            inner.AddAttribute(41, "class", "sc-ai-drawer__body");

            inner.OpenComponent<MessageList>(42);
            inner.AddComponentParameter(43, "Footer", (RenderFragment<AgentContext>)(ctx =>
                (RenderFragment)(footerBuilder =>
            {
                RenderDefaultFooter(footerBuilder, ctx, 0);
            })));
            inner.CloseComponent();

            inner.CloseElement(); // body

            // Footer
            inner.OpenElement(50, "div");
            inner.AddAttribute(51, "class", "sc-ai-drawer__footer");

            inner.OpenComponent<MessageInput>(52);
            inner.AddComponentParameter(53, "Placeholder", Placeholder);
            inner.AddComponentParameter(54, "AllowAttachments", AllowAttachments);
            inner.AddComponentParameter(55, "AcceptFileTypes", AcceptFileTypes);
            inner.CloseComponent();

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

    private async Task CloseAsync()
    {
        Open = false;
        await OpenChanged.InvokeAsync(false);
    }

    private string CssClass()
    {
        var position = Position == DrawerPosition.Left ? "left" : "right";
        var css = $"sc-ai-root sc-ai-drawer sc-ai-drawer--{position}";
        if (AdditionalAttributes?.TryGetValue("class", out var existing) == true && existing is string s)
        {
            css = $"{css} {s}";
        }
        return css;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Full page chat shell: an <see cref="AgentBoundary"/> around a scrollable
/// <see cref="MessageList"/> and a <see cref="MessageInput"/>.
/// </summary>
/// <example>
/// <code>
/// &lt;ChatPage Agent="agent" Placeholder="Ask me anything" /&gt;
/// </code>
/// </example>
public sealed class ChatPage : ComponentBase
{
    /// <summary>
    /// Gets or sets the agent that drives the conversation.
    /// </summary>
    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    /// <summary>
    /// Gets or sets the content rendered above the conversation.
    /// </summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>
    /// Gets or sets the content rendered when the conversation is empty.
    /// </summary>
    [Parameter]
    public RenderFragment? WelcomeContent { get; set; }

    /// <summary>
    /// Gets or sets the content rendered inside the message list, typically
    /// <see cref="BlockRenderer{TBlock}"/> registrations.
    /// </summary>
    [Parameter]
    public RenderFragment? MessageListContent { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text of the message input.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the content rendered before the message input text area.
    /// </summary>
    [Parameter]
    public RenderFragment? InputLeadingActions { get; set; }

    /// <summary>
    /// Gets or sets the content rendered after the message input text area.
    /// </summary>
    [Parameter]
    public RenderFragment? InputTrailingActions { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the root element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass());

        if (Header is not null)
        {
            builder.OpenElement(10, "div");
            builder.AddAttribute(11, "class", "sc-ai-chat-page__header");
            builder.AddContent(12, Header);
            builder.CloseElement();
        }

        builder.OpenComponent<AgentBoundary>(20);
        builder.AddComponentParameter(21, "Agent", Agent);
        builder.AddComponentParameter(22, "ChildContent", (RenderFragment)(inner =>
        {
            // Body (scrollable area)
            inner.OpenElement(30, "div");
            inner.AddAttribute(31, "class", "sc-ai-chat-page__body");

            inner.OpenComponent<MessageList>(32);
            inner.AddComponentParameter(33, "ChildContent", MessageListContent);
            inner.AddComponentParameter(34, "EmptyContent", WelcomeContent);
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
            inner.CloseComponent(); // MessageInput

            inner.CloseElement(); // input-container
            inner.CloseElement(); // footer
        }));
        builder.CloseComponent(); // AgentBoundary

        builder.CloseElement(); // root div
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
}

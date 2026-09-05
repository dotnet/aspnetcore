// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Submits the nearest <see cref="MessageInput"/>.
/// </summary>
public sealed class MessageSendButton : ComponentBase, IDisposable
{
    private MessageInputContext? _subscribedContext;
    private IDisposable? _changeSubscription;

    /// <summary>
    /// Gets or sets the nearest message input.
    /// </summary>
    [CascadingParameter]
    public MessageInputContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the accessible label for the button.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Send message";

    /// <summary>
    /// Gets or sets custom button content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the button.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribedContext, Context))
        {
            return;
        }

        _changeSubscription?.Dispose();
        _subscribedContext = Context;
        _changeSubscription = Context.RegisterOnChanged(
            () => _ = InvokeAsync(StateHasChanged));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "class", CssClass());
        builder.AddAttribute(4, "disabled", !Context.CanSubmit);
        builder.AddAttribute(5, "aria-label", Label);
        builder.AddAttribute(
            6,
            "onclick",
            EventCallback.Factory.Create(this, Context.SubmitAsync));

        if (ChildContent is not null)
        {
            builder.AddContent(7, ChildContent);
        }
        else
        {
            builder.AddMarkupContent(
                8,
                "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M22 2 11 13\"/><path d=\"M22 2 15 22 11 13 2 9z\"/></svg>");
        }

        builder.CloseElement();
    }

    private string CssClass()
    {
        var css = "sc-ai-input__send";
        if (AdditionalAttributes?.TryGetValue("class", out var value) == true &&
            value is string additionalClass)
        {
            css = $"{css} {additionalClass}";
        }

        return css;
    }

    /// <summary>
    /// Removes the composer state subscription.
    /// </summary>
    public void Dispose()
    {
        _changeSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}

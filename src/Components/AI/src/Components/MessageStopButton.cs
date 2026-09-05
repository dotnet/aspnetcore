// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Stops the response associated with the nearest <see cref="MessageInput"/>.
/// </summary>
public sealed class MessageStopButton : ComponentBase, IDisposable
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
    public string Label { get; set; } = "Stop response";

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
        if (!Context.CanCancel)
        {
            return;
        }

        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "class", CssClass());
        builder.AddAttribute(4, "aria-label", Label);
        builder.AddAttribute(
            5,
            "onclick",
            EventCallback.Factory.Create(this, Context.CancelAsync));
        builder.AddContent(6, ChildContent ?? (RenderFragment)(content => content.AddContent(0, "Stop")));
        builder.CloseElement();
    }

    private string CssClass()
    {
        var css = "sc-ai-input__stop";
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

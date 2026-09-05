// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Renders the attachments currently staged in a <see cref="MessageInput"/>.
/// </summary>
public sealed class MessageAttachmentList : ComponentBase, IDisposable
{
    private MessageInputContext? _subscribedContext;
    private IDisposable? _changeSubscription;

    /// <summary>
    /// Gets or sets the message input that owns the attachments.
    /// </summary>
    [CascadingParameter]
    public MessageInputContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the accessible label for the attachment list.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Attached files";

    /// <summary>
    /// Gets or sets additional attributes applied to the attachment list.
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
        if (Context.Attachments.Count == 0)
        {
            return;
        }

        builder.OpenElement(0, "ul");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass());
        builder.AddAttribute(3, "aria-label", Label);

        var sequence = 10;
        foreach (var attachment in Context.Attachments)
        {
            builder.OpenRegion(sequence++);
            builder.OpenElement(0, "li");
            builder.SetKey(attachment);

            builder.OpenComponent<MediaContent>(1);
            builder.AddComponentParameter(2, nameof(MediaContent.Content), attachment);
            builder.AddComponentParameter(
                3,
                nameof(MediaContent.AlternativeText),
                GetAlternativeText(attachment));
            builder.CloseComponent();

            builder.OpenElement(4, "span");
            builder.AddAttribute(5, "class", "sc-ai-attachment__name");
            builder.AddContent(6, GetName(attachment));
            builder.CloseElement();

            builder.OpenElement(7, "button");
            builder.AddAttribute(8, "type", "button");
            builder.AddAttribute(9, "class", "sc-ai-attachment__remove");
            builder.AddAttribute(10, "aria-label", $"Remove {GetName(attachment)}");
            builder.AddAttribute(
                11,
                "onclick",
                EventCallback.Factory.Create(
                    this,
                    () => Context.RemoveAttachmentAsync(attachment).AsTask()));
            builder.AddContent(12, "Remove");
            builder.CloseElement();

            builder.CloseElement();
            builder.CloseRegion();
        }

        builder.CloseElement();
    }

    private string CssClass()
    {
        var css = "sc-ai-attachments";
        if (AdditionalAttributes?.TryGetValue("class", out var value) == true &&
            value is string additionalClass)
        {
            css = $"{css} {additionalClass}";
        }

        return css;
    }

    private static string GetName(DataContent content)
    {
        return string.IsNullOrWhiteSpace(content.Name)
            ? "Attachment"
            : content.Name;
    }

    private static string GetAlternativeText(DataContent content)
    {
        return string.IsNullOrWhiteSpace(content.Name)
            ? "Attached image"
            : $"Attached image {content.Name}";
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

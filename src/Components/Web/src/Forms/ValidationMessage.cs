// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Displays a list of validation messages for a specified field within a cascaded <see cref="EditContext"/>.
/// </summary>
public class ValidationMessage<TValue> : ComponentBase, IDisposable
{
    private EditContext? _previousEditContext;
    private Expression<Func<TValue>>? _previousFieldAccessor;
    private readonly EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;
    private FieldIdentifier _fieldIdentifier;

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created <c>div</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] EditContext CurrentEditContext { get; set; } = default!;

    [CascadingParameter] private HtmlFieldPrefix? FieldPrefix { get; set; }

    /// <summary>
    /// Specifies the field for which validation messages should be displayed.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? For { get; set; }

    /// <summary>`
    /// Constructs an instance of <see cref="ValidationMessage{TValue}"/>.
    /// </summary>
    public ValidationMessage()
    {
        _validationStateChangedHandler = (sender, eventArgs) => StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                $"of type {nameof(EditContext)}. For example, you can use {GetType()} inside " +
                $"an {nameof(EditForm)}.");
        }

        if (For == null) // Not possible except if you manually specify T
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                $"{nameof(For)} parameter.");
        }
        else if (For != _previousFieldAccessor)
        {
            _fieldIdentifier = FieldIdentifier.Create(For);
            _previousFieldAccessor = For;
        }

        if (CurrentEditContext != _previousEditContext)
        {
            DetachValidationStateChangedListener();
            CurrentEditContext.OnValidationStateChanged += _validationStateChangedHandler;
            _previousEditContext = CurrentEditContext;
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (AssignedRenderMode is not null)
        {
            // Interactive .NET validation
            RenderInteractiveRenderTree(builder);
        }
        else
        {
            // Client-side JS validation
            RenderStaticRenderTree(builder);
        }
    }

    private void RenderInteractiveRenderTree(RenderTreeBuilder builder)
    {
        foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddMultipleAttributes(2, AdditionalAttributes);
            builder.AddContent(3, message);
            builder.CloseElement();
        }
    }

    private void RenderStaticRenderTree(RenderTreeBuilder builder)
    {
        var fieldName = FieldPrefix?.GetFieldName(For!) ?? ExpressionFormatter.FormatLambda(For!);
        var first = true;

        foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            if (first)
            {
                // First message element carries data-valmsg-for/replace so the JS client-validation
                // engine can locate it and swap its content with client-side messages.
                builder.AddAttribute(2, "data-valmsg-for", fieldName);
                builder.AddAttribute(3, "data-valmsg-replace", "true");
                first = false;
            }
            builder.AddMultipleAttributes(4, AdditionalAttributes);
            builder.AddContent(5, message);
            builder.CloseElement();
        }

        if (first)
        {
            // No server-side messages: render a hidden placeholder so the JS engine has an element
            // to populate when client validation runs.
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddAttribute(2, "data-valmsg-for", fieldName);
            builder.AddAttribute(3, "data-valmsg-replace", "true");
            // Use the hidden attribute rather than an inline style="display:none" for CSP compliance.
            builder.AddAttribute(4, "hidden", true);
            builder.AddMultipleAttributes(5, AdditionalAttributes);
            builder.CloseElement();
        }
    }

    /// <summary>
    /// Called to dispose this instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if called within <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        DetachValidationStateChangedListener();
        Dispose(disposing: true);
    }

    private void DetachValidationStateChangedListener()
    {
        _previousEditContext?.OnValidationStateChanged -= _validationStateChangedHandler;
    }
}

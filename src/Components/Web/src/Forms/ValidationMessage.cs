// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;
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
        var hasClientValidation = CurrentEditContext.Properties.TryGetValue(typeof(IClientValidationService), out var serviceObj)
            && serviceObj is IClientValidationService;

        if (hasClientValidation)
        {
            RenderForClientValidation(builder);
            return;
        }

        foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddMultipleAttributes(2, AdditionalAttributes);
            builder.AddContent(3, message);
            builder.CloseElement();
        }
    }

    /// <summary>
    /// Renders validation messages with data-valmsg-for and data-valmsg-replace attributes
    /// for the JS validation library. The first message element carries the attributes so the
    /// JS library can find it and replace its content. If no server-side messages exist yet,
    /// an empty placeholder div is rendered for JS to populate when client validation runs.
    /// Server-rendered sibling messages (without data-valmsg-for) are cleaned up by the JS library
    /// when it inserts the client-side validation messages.
    /// </summary>
    private void RenderForClientValidation(RenderTreeBuilder builder)
    {
        // Use HtmlFieldPrefix to compute the field name consistently with InputBase.
        // In nested Editor scenarios, FieldPrefix adjusts the name to match the rendered input's name attribute.
        var fieldName = FieldPrefix?.GetFieldName(For!) ?? ExpressionFormatter.FormatLambda(For!);
        var first = true;

        foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            if (first)
            {
                // First message element will be used as container for client-side validation message
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
            // No messages - render empty placeholder for JS to find
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddAttribute(2, "data-valmsg-for", fieldName);
            builder.AddAttribute(3, "data-valmsg-replace", "true");
            builder.AddMultipleAttributes(4, AdditionalAttributes);
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

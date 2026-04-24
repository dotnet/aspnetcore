// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

// Note: there's no reason why developers strictly need to use this. It's equally valid to
// put a @foreach(var message in context.GetValidationMessages()) { ... } inside a form.
// This component is for convenience only, plus it implements a few small perf optimizations.

/// <summary>
/// Displays a list of validation messages from a cascaded <see cref="EditContext"/>.
/// </summary>
public class ValidationSummary : ComponentBase, IDisposable
{
    private EditContext? _previousEditContext;
    private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;

    /// <summary>
    /// Gets or sets the model to produce the list of validation messages for.
    /// When specified, this lists all errors that are associated with the model instance.
    /// </summary>
    [Parameter] public object? Model { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created <c>ul</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] EditContext CurrentEditContext { get; set; } = default!;

    /// <summary>`
    /// Constructs an instance of <see cref="ValidationSummary"/>.
    /// </summary>
    public ValidationSummary()
    {
        _validationStateChangedHandler = (sender, eventArgs) => StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(ValidationSummary)} requires a cascading parameter " +
                $"of type {nameof(EditContext)}. For example, you can use {nameof(ValidationSummary)} inside " +
                $"an {nameof(EditForm)}.");
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
        // As an optimization, only evaluate the messages enumerable once, and
        // only produce the enclosing <ul> if there's at least one message,
        // or client-side validation is enabled.
        var validationMessages = Model is null ?
            CurrentEditContext.GetValidationMessages() :
            CurrentEditContext.GetValidationMessages(new FieldIdentifier(Model, string.Empty));

        var hasClientValidation = CurrentEditContext.Properties.TryGetValue(typeof(IClientValidationService), out var serviceObj)
            && serviceObj is IClientValidationService;

        if (hasClientValidation)
        {
            RenderForClientValidation(builder, validationMessages);
            return;
        }

        var first = true;
        foreach (var error in validationMessages)
        {
            if (first)
            {
                first = false;

                builder.OpenElement(0, "ul");
                builder.AddAttribute(1, "class", "validation-errors");
                builder.AddMultipleAttributes(2, AdditionalAttributes);
            }

            builder.OpenElement(3, "li");
            builder.AddAttribute(4, "class", "validation-message");
            builder.AddContent(5, error);
            builder.CloseElement();
        }

        if (!first)
        {
            // We have at least one validation message.
            builder.CloseElement();
        }
    }

    /// <summary>
    /// Renders a validation summary container with data-valmsg-summary="true" for the JS
    /// validation library. Sets the initial CSS class based on whether server-rendered messages
    /// exist: validation-summary-errors when non-empty (so CSS that hides validation-summary-valid
    /// won't suppress initial server errors), validation-summary-valid when empty.
    /// </summary>
    private void RenderForClientValidation(RenderTreeBuilder builder, IEnumerable<string> validationMessages)
    {
        var messages = new List<string>(validationMessages);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-valmsg-summary", "true");
        builder.AddAttribute(2, "class", messages.Count > 0 ? "validation-summary-errors" : "validation-summary-valid");

        builder.OpenElement(3, "ul");
        builder.AddAttribute(4, "class", "validation-errors");
        builder.AddMultipleAttributes(5, AdditionalAttributes);

        foreach (var error in messages)
        {
            builder.OpenElement(6, "li");
            builder.AddAttribute(7, "class", "validation-message");
            builder.AddContent(8, error);
            builder.CloseElement();
        }

        builder.CloseElement(); // ul
        builder.CloseElement(); // div
    }

    /// <inheritdoc/>
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private IEnumerable<string> GetValidationMessages()
        => Model is null
            ? CurrentEditContext.GetValidationMessages()
            : CurrentEditContext.GetValidationMessages(new FieldIdentifier(Model, string.Empty));

    private void RenderInteractiveRenderTree(RenderTreeBuilder builder)
    {
        // As an optimization, only evaluate the messages enumerable once, and
        // only produce the enclosing <ul> if there's at least one message.
        var first = true;
        foreach (var error in GetValidationMessages())
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

    private void RenderStaticRenderTree(RenderTreeBuilder builder)
    {
        var messages = new List<string>(GetValidationMessages());
        var hasErrors = messages.Count > 0;

        // The <ul> itself is the carrier the JS client-validation engine locates via
        // data-valmsg-summary. The state class starts as validation-summary-errors/-valid so
        // server-rendered errors are styled before the JS engine runs.
        builder.OpenElement(0, "ul");
        builder.AddAttribute(1, "class", hasErrors
            ? "validation-errors validation-summary-errors"
            : "validation-errors validation-summary-valid");
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "data-valmsg-summary", "true");
        if (!hasErrors)
        {
            // No server-side messages: keep the empty summary out of the layout until the JS engine
            // populates it.
            // Use the hidden attribute rather than an inline style="display:none" for CSP compliance.
            builder.AddAttribute(4, "hidden", true);
        }

        foreach (var error in messages)
        {
            builder.OpenElement(5, "li");
            builder.AddAttribute(6, "class", "validation-message");
            builder.AddContent(7, error);
            builder.CloseElement();
        }

        builder.CloseElement(); // ul
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

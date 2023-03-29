// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Adds model binding validation support to an <see cref="EditContext"/>.
/// </summary>
public class BindingContextValidator : ComponentBase, IDisposable
{
    private IDisposable? _subscriptions;
    private EditContext? _originalEditContext;

    [CascadingParameter] public EditContext? CurrentEditContext { get; set; }

    [CascadingParameter] public BindingContext BindingContext { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(BindingContextValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}.");
        }

        if (BindingContext == null)
        {
            return;
        }

        _subscriptions = CurrentEditContext.EnableBindingContextExtensions(BindingContext);
        _originalEditContext = CurrentEditContext;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (BindingContext == null)
        {
            return;
        }

        if (CurrentEditContext != _originalEditContext)
        {
            // While we could support this, there's no known use case presently. Since InputBase doesn't support it,
            // it's more understandable to have the same restriction.
            throw new InvalidOperationException($"{GetType()} does not support changing the " +
                $"{nameof(EditContext)} dynamically.");
        }
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        _subscriptions?.Dispose();
        _subscriptions = null;

        Dispose(disposing: true);
    }
}

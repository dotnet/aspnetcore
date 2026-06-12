// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

/// <summary>
/// Exposes validation messages for a given <see cref="FormMappingContext"/>.
/// </summary>
internal class FormMappingValidator : ComponentBase, IDisposable
{
    private IDisposable? _subscriptions;
    private EditContext? _originalEditContext;

    [Parameter] public EditContext? CurrentEditContext { get; set; }

    [CascadingParameter] internal FormMappingContext? MappingContext { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(FormMappingValidator)} requires a " +
                $"parameter of type {nameof(EditContext)}.");
        }

        if (MappingContext == null)
        {
            return;
        }

        _subscriptions = CurrentEditContext.EnableFormMappingContextExtensions(MappingContext);
        _originalEditContext = CurrentEditContext;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (MappingContext == null)
        {
            return;
        }

        if (CurrentEditContext != _originalEditContext)
        {
            // The EditContext changed (e.g. because EditForm.AllowModelChange=true replaced the
            // model with a new instance). Dispose the old subscriptions and re-subscribe using
            // the new context so that form-mapping validation continues to work correctly.
            _subscriptions?.Dispose();
            _subscriptions = CurrentEditContext!.EnableFormMappingContextExtensions(MappingContext);
            _originalEditContext = CurrentEditContext;
        }
    }

    void IDisposable.Dispose()
    {
        _subscriptions?.Dispose();
        _subscriptions = null;
    }
}

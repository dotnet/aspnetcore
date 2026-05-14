// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.ClientValidation;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Adds Data Annotations validation support to an <see cref="EditContext"/>.
/// When rendering in a static SSR context, also activates client-side validation
/// by storing an <see cref="IClientValidationService"/> on the <see cref="EditContext.Properties"/>.
/// </summary>
public class DataAnnotationsValidator : ComponentBase, IDisposable
{
    private IDisposable? _subscriptions;
    private EditContext? _originalEditContext;

    [CascadingParameter] EditContext? CurrentEditContext { get; set; }

    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether client-side validation attributes (data-val-*) should be emitted
    /// on input components within this form. Default is <c>true</c>.
    /// </summary>
    [Parameter] public bool EnableClientValidation { get; set; } = true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                $"inside an EditForm.");
        }

        _subscriptions = CurrentEditContext.EnableDataAnnotationsValidation(ServiceProvider);
        _originalEditContext = CurrentEditContext;

        // Enable client-side validation only in static SSR context.
        // AssignedRenderMode is null when rendering statically (no interactive mode assigned).
        if (EnableClientValidation && AssignedRenderMode is null)
        {
            if (ServiceProvider.GetService(typeof(IClientValidationService)) is { } service)
            {
                CurrentEditContext.Properties[typeof(IClientValidationService)] = service;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext != _originalEditContext)
        {
            // While we could support this, there's no known use case presently. Since InputBase doesn't support it,
            // it's more understandable to have the same restriction.
            throw new InvalidOperationException($"{GetType()} does not support changing the " +
                $"{nameof(EditContext)} dynamically.");
        }
    }

    /// <summary>
    /// Releases resources used by the validator.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if managed resources should be released; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        _subscriptions?.Dispose();
        _subscriptions = null;

        // Clean up the client validation service reference from EditContext
        CurrentEditContext?.Properties.Remove(typeof(IClientValidationService));

        Dispose(disposing: true);
    }
}

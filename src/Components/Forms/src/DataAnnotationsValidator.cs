// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [Inject] private IClientValidationService? ClientValidationService { get; set; }

    /// <summary>
    /// Gets or sets whether client-side validation is enabled for the form.
    /// Defaults to <see langword="true"/>. Set to <see langword="false"/> to disable
    /// client-side validation while keeping server-side DataAnnotations validation active.
    /// </summary>
    [Parameter]
    public bool EnableClientValidation { get; set; } = true;

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

        if (EnableClientValidation && ClientValidationService is not null && AssignedRenderMode is null)
        {
            CurrentEditContext.Properties[typeof(IClientValidationService)] = ClientValidationService;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext != _originalEditContext)
        {
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

        CurrentEditContext?.Properties.Remove(typeof(IClientValidationService));

        Dispose(disposing: true);
    }
}

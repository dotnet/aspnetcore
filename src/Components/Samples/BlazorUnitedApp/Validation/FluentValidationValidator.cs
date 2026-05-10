// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorUnitedApp.Validation;

/// <summary>
/// Proof-of-concept validator component that bridges FluentValidation into Blazor's
/// async <see cref="EditContext"/> pipeline. Subscribes to:
/// <list type="bullet">
/// <item><see cref="EditContext.OnValidationRequestedAsync"/> for full-form validation
/// (driven by <see cref="EditContext.ValidateAsync"/>).</item>
/// <item><see cref="EditContext.OnFieldChanged"/> for per-field validation as the user edits.</item>
/// </list>
/// Per-field validations are registered via <see cref="EditContext.AddValidationTask"/> so
/// the framework tracks pending/faulted state and supersedes stale runs automatically.
/// </summary>
public sealed class FluentValidationValidator<TModel> : ComponentBase, IDisposable
    where TModel : class
{
    private EditContext? _previousEditContext;
    private ValidationMessageStore? _messages;
    private IValidator<TModel>? _validator;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FluentValidationValidator<TModel>)} requires a cascading parameter of type EditContext. " +
                "It should be placed inside an EditForm.");
        }

        if (CurrentEditContext == _previousEditContext)
        {
            return;
        }

        DetachHandlers();

        _validator = Services.GetRequiredService<IValidator<TModel>>();
        _messages = new ValidationMessageStore(CurrentEditContext);
        CurrentEditContext.OnValidationRequestedAsync += ValidateModelAsync;
        CurrentEditContext.OnFieldChanged += FieldChanged;
        _previousEditContext = CurrentEditContext;
    }

    private async Task ValidateModelAsync(object sender, ValidationRequestedEventArgs args)
    {
        var editContext = (EditContext)sender;
        var messages = _messages!;
        var validator = _validator!;

        var result = await validator.ValidateAsync((TModel)editContext.Model, args.CancellationToken);

        messages.Clear();

        foreach (var error in result.Errors)
        {
            // Map FV's dotted path (e.g. "Address.Street") to the leaf-model FieldIdentifier the
            // ValidationMessage components expect. Falls back to a path-keyed identifier on the
            // root if the path can't be resolved (e.g. the model graph changed mid-validation).
            if (FieldPath.TryDecode(editContext.Model, error.PropertyName, out var field))
            {
                messages.Add(field, error.ErrorMessage);
            }
            else
            {
                messages.Add(editContext.Field(error.PropertyName), error.ErrorMessage);
            }
        }

        editContext.NotifyValidationStateChanged();
    }

    private void FieldChanged(object? sender, FieldChangedEventArgs args)
    {
        var editContext = CurrentEditContext!;

        // Encode the changed field's leaf-model identifier back to a dotted path so FV can scope
        // its work to the touched property. Skip if the field isn't reachable from the root model
        // graph (could happen for transient FieldIdentifiers not produced by InputBase).
        if (!FieldPath.TryEncode(editContext.Model, args.FieldIdentifier, out var path))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        var task = ValidateFieldAsync(args.FieldIdentifier, path, cts.Token);
        editContext.AddValidationTask(args.FieldIdentifier, task, cts);
    }

    private async Task ValidateFieldAsync(FieldIdentifier field, string propertyPath, CancellationToken cancellationToken)
    {
        var editContext = CurrentEditContext!;
        var messages = _messages!;
        var validator = _validator!;

        // Clear stale messages for this field up-front so the field shows neutral state during
        // validation and after a throw or cancellation. Fault state is signalled separately
        // by IsValidationFaulted(field).
        messages.Clear(field);
        editContext.NotifyValidationStateChanged();

        var context = ValidationContext<TModel>.CreateWithOptions(
            (TModel)editContext.Model,
            options => options.IncludeProperties(propertyPath));

        var result = await validator.ValidateAsync(context, cancellationToken);

        foreach (var error in result.Errors)
        {
            if (FieldPath.TryDecode(editContext.Model, error.PropertyName, out var errorField))
            {
                messages.Add(errorField, error.ErrorMessage);
            }
            else
            {
                messages.Add(editContext.Field(error.PropertyName), error.ErrorMessage);
            }
        }

        editContext.NotifyValidationStateChanged();
    }

    private void DetachHandlers()
    {
        if (_previousEditContext is not null)
        {
            _previousEditContext.OnValidationRequestedAsync -= ValidateModelAsync;
            _previousEditContext.OnFieldChanged -= FieldChanged;
            _messages?.Clear();
            _messages = null;
            _previousEditContext = null;
        }
    }

    public void Dispose() => DetachHandlers();
}

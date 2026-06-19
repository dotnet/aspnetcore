// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

[assembly: MetadataUpdateHandler(typeof(Microsoft.AspNetCore.Components.Forms.EditContextDataAnnotationsExtensions))]

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Extension methods to add DataAnnotations validation to an <see cref="EditContext"/>.
/// </summary>
public static partial class EditContextDataAnnotationsExtensions
{
    /// <summary>
    /// Enables DataAnnotations validation support for the <see cref="EditContext"/>.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used in the <see cref="ValidationContext"/>.</param>
    /// <returns>A disposable object whose disposal will remove DataAnnotations validation support from the <see cref="EditContext"/>.</returns>
    public static IDisposable EnableDataAnnotationsValidation(this EditContext editContext, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return new DataAnnotationsEventSubscriptions(editContext, serviceProvider);
    }

    private static event Action? OnClearCache;

#pragma warning disable IDE0051 // Remove unused private members
    private static void ClearCache(Type[]? _)
    {
        OnClearCache?.Invoke();
    }
#pragma warning restore IDE0051 // Remove unused private members

    private sealed partial class DataAnnotationsEventSubscriptions : IDisposable
    {
        private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> _propertyInfoCache = new();

        private readonly EditContext _editContext;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ValidationMessageStore _messages;
        private readonly ValidationOptions? _validationOptions;
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly IValidatableTypeInfo? _validatorTypeInfo;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly Dictionary<string, FieldIdentifier> _validationPathToFieldIdentifierMapping = new();

        [UnconditionalSuppressMessage("Trimming", "IL2066", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        public DataAnnotationsEventSubscriptions(EditContext editContext, IServiceProvider serviceProvider)
        {
            _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
            _serviceProvider = serviceProvider;
            _messages = new ValidationMessageStore(_editContext);
            _validationOptions = _serviceProvider?.GetService<IOptions<ValidationOptions>>()?.Value;
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _validatorTypeInfo = _validationOptions != null && _validationOptions.TryGetValidatableTypeInfo(_editContext.Model.GetType(), out var typeInfo)
                ? typeInfo
                : null;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _editContext.OnFieldChanged += OnFieldChanged;
            _editContext.OnValidationRequested += OnValidationRequested;

            if (MetadataUpdater.IsSupported)
            {
                OnClearCache += ClearCache;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
        {
            var fieldIdentifier = eventArgs.FieldIdentifier;
            var modelType = fieldIdentifier.Model.GetType();

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (_validationOptions is not null &&
                _validationOptions.TryGetValidatablePropertyInfo(modelType, fieldIdentifier.FieldName, out var validatablePropertyInfo))
            {
                _editContext.TrackFieldValidation(
                    fieldIdentifier,
                    token => ValidateFieldWithValidatableInfoAsync(fieldIdentifier, validatablePropertyInfo, token));
            }
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            else if (TryGetValidatableProperty(fieldIdentifier, out var propertyInfo))
            {
                _editContext.TrackFieldValidation(
                    fieldIdentifier,
                    token => ValidateFieldWithValidatorAsync(fieldIdentifier, propertyInfo, token));
            }
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            // Always register the form validation as a task.
            // ValidateAsync awaits it, Validate drains it when it completed synchronously
            // or throws when it did not.
            e.AddValidationTask(ValidateFormAndNotifyAsync(e.CancellationToken));
        }

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private async Task ValidateFormAndNotifyAsync(CancellationToken cancellationToken)
        {
            if (_validatorTypeInfo is not null)
            {
                await ValidateFormWithValidatableInfoAsync(_validatorTypeInfo, cancellationToken);
            }
            else
            {
                await ValidateFormWithValidatorAsync(cancellationToken);
            }

            _editContext.NotifyValidationStateChanged();
        }
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFormWithValidatorAsync(CancellationToken cancellationToken)
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
            var validationResults = new List<ValidationResult>();

            // Clear stale messages up-front so the form shows neutral state while validating and after
            // a throw or cancellation. Any faulted state is signalled separately via EditContext.IsValidationFaulted.
            _messages.Clear();

            await Validator.TryValidateObjectAsync(_editContext.Model, validationContext, validationResults, validateAllProperties: true, cancellationToken);

            // Transfer results to the ValidationMessageStore
            foreach (var validationResult in validationResults)
            {
                if (validationResult == null)
                {
                    continue;
                }

                var hasMemberNames = false;
                foreach (var memberName in validationResult.MemberNames)
                {
                    hasMemberNames = true;
                    _messages.Add(_editContext.Field(memberName), validationResult.ErrorMessage!);
                }

                if (!hasMemberNames)
                {
                    _messages.Add(new FieldIdentifier(_editContext.Model, fieldName: string.Empty), validationResult.ErrorMessage!);
                }
            }
        }

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFormWithValidatableInfoAsync(IValidatableTypeInfo validatableInfo, CancellationToken cancellationToken)
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
            var validateContext = new ValidateContext
            {
                ValidationOptions = _validationOptions!,
                ValidationContext = validationContext,
            };

            // Clear stale messages up-front. If the validator throws partway through, the form
            // shows no per-field messages (form-level fault state is signaled separately via
            // EditContext.IsValidationFaulted).
            _messages.Clear();

            try
            {
                validateContext.OnValidationError += AddMapping;

                await validatableInfo.ValidateAsync(_editContext.Model, validateContext, cancellationToken);

                if (validateContext.ValidationErrors is { Count: > 0 } validationErrors)
                {
                    foreach (var (fieldKey, messages) in validationErrors)
                    {
                        var fieldIdentifier = _validationPathToFieldIdentifierMapping[fieldKey];
                        _messages.Add(fieldIdentifier, messages);
                    }
                }
            }
            finally
            {
                validateContext.OnValidationError -= AddMapping;
                _validationPathToFieldIdentifierMapping.Clear();
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFieldWithValidatorAsync(FieldIdentifier fieldIdentifier, PropertyInfo propertyInfo, CancellationToken cancellationToken)
        {
            var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
            var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null)
            {
                MemberName = propertyInfo.Name
            };
            var results = new List<ValidationResult>();

            // Clear stale messages up-front so the field shows neutral state during validation and
            // after a throw or cancellation. Any faulted state is signalled separately via EditContext.IsValidationFaulted.
            _messages.Clear(fieldIdentifier);

            try
            {
                await Validator.TryValidatePropertyAsync(propertyValue, validationContext, results, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Task was cancelled (user re-edited field or form is submitting). The notification
                // emitted by the superseding TrackFieldValidation call already reflects the cleared
                // messages, so no extra notification is needed here.
                return;
            }

            foreach (var result in CollectionsMarshal.AsSpan(results))
            {
                _messages.Add(fieldIdentifier, result.ErrorMessage!);
            }

            // We have to notify even if there were no messages before and are still no messages now,
            // because the "state" that changed might be the completion of some async validation task
            _editContext.NotifyValidationStateChanged();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFieldWithValidatableInfoAsync(
            FieldIdentifier fieldIdentifier,
            IValidatablePropertyInfo validatableInfo,
            CancellationToken cancellationToken)
        {
            var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null);
            var validateContext = new ValidateContext
            {
                ValidationOptions = _validationOptions!,
                ValidationContext = validationContext,
            };

            // Clear stale messages up-front so the field shows neutral state during validation and
            // after a throw or cancellation. Any faulted state is signalled separately via EditContext.IsValidationFaulted.
            _messages.Clear(fieldIdentifier);

            try
            {
                await validatableInfo.ValidateAsync(fieldIdentifier.Model, validateContext, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Task was cancelled (user re-edited field or form is submitting). The notification
                // emitted by the superseding TrackFieldValidation call already reflects the cleared
                // messages, so no extra notification is needed here.
                return;
            }

            if (validateContext.ValidationErrors is { Count: > 0 } validationErrors)
            {
                foreach (var (_, messages) in validationErrors)
                {
                    _messages.Add(fieldIdentifier, messages);
                }
            }

            _editContext.NotifyValidationStateChanged();
        }

        private void AddMapping(ValidationErrorContext context)
        {
            _validationPathToFieldIdentifierMapping[context.Path] =
                new FieldIdentifier(context.Container ?? _editContext.Model, context.Name);
        }

        public void Dispose()
        {
            _messages.Clear();
            _editContext.OnFieldChanged -= OnFieldChanged;
            _editContext.OnValidationRequested -= OnValidationRequested;
            _editContext.NotifyValidationStateChanged();

            if (MetadataUpdater.IsSupported)
            {
                OnClearCache -= ClearCache;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private static bool TryGetValidatableProperty(in FieldIdentifier fieldIdentifier, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
        {
            var cacheKey = (ModelType: fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
            if (!_propertyInfoCache.TryGetValue(cacheKey, out propertyInfo))
            {
                // DataAnnotations only validates public properties, so that's all we'll look for
                // If we can't find it, cache 'null' so we don't have to try again next time
                propertyInfo = cacheKey.ModelType.GetProperty(cacheKey.FieldName);

                // No need to lock, because it doesn't matter if we write the same value twice
                _propertyInfoCache[cacheKey] = propertyInfo;
            }

            return propertyInfo != null;
        }

        internal void ClearCache()
        {
            _propertyInfoCache.Clear();
        }
    }
}
